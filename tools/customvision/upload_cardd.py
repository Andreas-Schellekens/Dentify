"""
Uploads a balanced selection of the CarDD dataset (COCO format) to an Azure Custom Vision
object detection project, including the bounding boxes, so no manual labeling is needed.

Uses only the Python standard library and the Custom Vision Training REST API v3.3
(checked against the official OpenAPI spec in Azure/azure-rest-api-specs).

Configuration comes from environment variables, never from this file:
    CUSTOMVISION_TRAINING_ENDPOINT   e.g. https://westeurope.api.cognitive.microsoft.com/
    CUSTOMVISION_TRAINING_KEY        training key of the Custom Vision training resource
    CUSTOMVISION_PROJECT_ID          project id (gear icon in customvision.ai)

Examples:
    python upload_cardd.py --cardd-dir "C:/Users/me/Downloads/CarDD/CarDD_release/CarDD_COCO" --dry-run
    python upload_cardd.py --cardd-dir "..." --per-tag 60
    python upload_cardd.py --cardd-dir "..." --export-test 15 --test-dir "C:/Users/me/Downloads/CarDD/quick-test"
"""

import argparse
import base64
import json
import os
import random
import shutil
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from collections import defaultdict

API_PATH = "customvision/v3.3/training"
MAX_IMAGES_PER_BATCH = 64       # API limit
UPLOAD_BATCH_SIZE = 8           # keeps each request small (images are base64 encoded)
MAX_TRAINING_IMAGES_F0 = 5000   # Free tier limit per project

# CarDD category name -> Custom Vision tag name
TAG_MAP = {
    "scratch": "scratch",
    "dent": "dent",
    "crack": "crack",
    "glass shatter": "broken-glass",
    "lamp broken": "broken-lamp",
    "tire flat": "flat-tire",
}


def load_coco(cardd_dir, split):
    path = os.path.join(cardd_dir, "annotations", f"instances_{split}.json")
    with open(path, encoding="utf-8") as f:
        data = json.load(f)

    categories = {c["id"]: c["name"] for c in data["categories"]}
    images = {}
    for image in data["images"]:
        images[image["id"]] = {
            "path": os.path.join(cardd_dir, split, image["file_name"]),
            "name": f"cardd_{split}_{image['file_name']}",
            "width": image["width"],
            "height": image["height"],
            "boxes": [],
        }

    for annotation in data["annotations"]:
        tag = TAG_MAP.get(categories[annotation["category_id"]])
        if tag is None:
            continue
        images[annotation["image_id"]]["boxes"].append((tag, annotation["bbox"]))

    return [image for image in images.values() if image["boxes"]]


def select_balanced(images, per_tag, seed):
    """Greedy selection: rarest tags first, until every tag appears on at least per_tag images."""
    rng = random.Random(seed)
    rng.shuffle(images)

    images_by_tag = defaultdict(list)
    for image in images:
        for tag in {tag for tag, _ in image["boxes"]}:
            images_by_tag[tag].append(image)

    selected = {}
    count_per_tag = defaultdict(int)
    for tag in sorted(images_by_tag, key=lambda t: len(images_by_tag[t])):
        for image in images_by_tag[tag]:
            if count_per_tag[tag] >= per_tag:
                break
            if image["name"] in selected:
                continue
            selected[image["name"]] = image
            for t in {t for t, _ in image["boxes"]}:
                count_per_tag[t] += 1

    return list(selected.values()), dict(count_per_tag)


def to_region(tag_id, bbox, width, height):
    # COCO bbox = [x, y, w, h] in pixels; Custom Vision wants fractions between 0 and 1
    x, y, w, h = bbox
    left = min(max(x / width, 0.0), 1.0)
    top = min(max(y / height, 0.0), 1.0)
    return {
        "tagId": tag_id,
        "left": left,
        "top": top,
        "width": min(w / width, 1.0 - left),
        "height": min(h / height, 1.0 - top),
    }


class TrainingClient:
    def __init__(self, endpoint, key, project_id):
        self.base = f"{endpoint.rstrip('/')}/{API_PATH}/projects/{project_id}"
        self.key = key

    def _request(self, method, path, query=None, body=None):
        url = self.base + path
        if query:
            url += "?" + urllib.parse.urlencode(query)
        data = json.dumps(body).encode("utf-8") if body is not None else None
        request = urllib.request.Request(url, data=data, method=method)
        request.add_header("Training-Key", self.key)
        if data is not None:
            request.add_header("Content-Type", "application/json")

        for attempt in range(5):
            try:
                with urllib.request.urlopen(request, timeout=300) as response:
                    text = response.read().decode("utf-8")
                    return json.loads(text) if text else None
            except urllib.error.HTTPError as error:
                if error.code == 429 and attempt < 4:   # rate limited, wait and retry
                    time.sleep(2 ** attempt)
                    continue
                detail = error.read().decode("utf-8", errors="replace")
                raise RuntimeError(f"{method} {path} failed with HTTP {error.code}: {detail}") from None

    def get_project(self):
        return self._request("GET", "")

    def get_tags(self):
        return self._request("GET", "/tags")

    def create_tag(self, name):
        return self._request("POST", "/tags", query={"name": name})

    def create_images_from_files(self, entries):
        return self._request("POST", "/images/files", body={"images": entries})


def export_test_images(cardd_dir, count, test_dir, seed):
    images = load_coco(cardd_dir, "test2017")
    random.Random(seed).shuffle(images)
    os.makedirs(test_dir, exist_ok=True)
    for image in images[:count]:
        shutil.copy2(image["path"], os.path.join(test_dir, os.path.basename(image["path"])))
    print(f"Copied {min(count, len(images))} test images to {test_dir}")


def main():
    parser = argparse.ArgumentParser(description="Upload CarDD to Azure Custom Vision with bounding boxes.")
    parser.add_argument("--cardd-dir", required=True, help="Path to the CarDD_COCO folder")
    parser.add_argument("--per-tag", type=int, default=60, help="Minimum number of images per tag (default 60)")
    parser.add_argument("--splits", nargs="+", default=["train2017", "val2017"])
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--dry-run", action="store_true", help="Only show the selection, upload nothing")
    parser.add_argument("--export-test", type=int, default=0, help="Copy N unseen test images for Quick Test")
    parser.add_argument("--test-dir", help="Target folder for --export-test")
    args = parser.parse_args()

    if args.export_test:
        if not args.test_dir:
            sys.exit("--export-test needs --test-dir")
        export_test_images(args.cardd_dir, args.export_test, args.test_dir, args.seed)
        return

    images = []
    for split in args.splits:
        images.extend(load_coco(args.cardd_dir, split))

    selected, count_per_tag = select_balanced(images, args.per_tag, args.seed)
    region_count = sum(len(image["boxes"]) for image in selected)
    print(f"Selected {len(selected)} images with {region_count} regions")
    for tag in sorted(count_per_tag):
        print(f"  {tag:<14} on {count_per_tag[tag]} images")

    if len(selected) > MAX_TRAINING_IMAGES_F0:
        sys.exit(f"Selection exceeds the F0 limit of {MAX_TRAINING_IMAGES_F0} images, lower --per-tag")
    if args.dry_run:
        print("Dry run, nothing uploaded.")
        return

    try:
        client = TrainingClient(os.environ["CUSTOMVISION_TRAINING_ENDPOINT"],
                                os.environ["CUSTOMVISION_TRAINING_KEY"],
                                os.environ["CUSTOMVISION_PROJECT_ID"])
    except KeyError as missing:
        sys.exit(f"Environment variable {missing} is not set")

    project = client.get_project()
    print(f"Project: {project['name']}")

    tag_ids = {tag["name"]: tag["id"] for tag in client.get_tags()}
    for tag in sorted(set(TAG_MAP.values())):
        if tag not in tag_ids:
            tag_ids[tag] = client.create_tag(tag)["id"]
            print(f"Created tag {tag}")

    counts = defaultdict(int)
    batch_size = min(UPLOAD_BATCH_SIZE, MAX_IMAGES_PER_BATCH)
    for start in range(0, len(selected), batch_size):
        entries = []
        for image in selected[start:start + batch_size]:
            with open(image["path"], "rb") as f:
                contents = base64.b64encode(f.read()).decode("ascii")
            regions = [to_region(tag_ids[tag], bbox, image["width"], image["height"])
                       for tag, bbox in image["boxes"]]
            entries.append({"name": image["name"], "contents": contents, "regions": regions})

        summary = client.create_images_from_files(entries)
        for result in summary["images"]:
            counts[result["status"]] += 1
            if result["status"] not in ("OK", "OKDuplicate"):
                print(f"  {result.get('sourceUrl')}: {result['status']}")

        done = min(start + batch_size, len(selected))
        print(f"Uploaded {done}/{len(selected)}")
        time.sleep(0.3)

    print("Done:", dict(counts))


if __name__ == "__main__":
    main()
