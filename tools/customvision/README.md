# Uploading CarDD to Custom Vision

`upload_cardd.py` uploads a balanced selection of the [CarDD](https://cardd-ustc.github.io/) dataset to an Azure Custom Vision project, with the labels already filled in, so nothing has to be labeled by hand. It only uses the Python standard library.

CarDD is licensed for non-commercial research and education. Credit it in the README and the presentation: *Wang, Li, Wu – "CarDD: A New Dataset for Vision-Based Car Damage Detection", IEEE TITS 2023.*

## 1. Prerequisites (once per PC)

- Python 3 (`python --version`).
- The CarDD zip, extracted so this folder exists: `...\CarDD\CarDD_release\CarDD_COCO\` with `annotations\`, `train2017\`, `val2017\` and `test2017\`. Only the `CarDD_COCO` part is needed (about 3 GB).

Tip: the upload does not have to run on the PC of the person who owns the Azure resource. Whoever has the dataset can run it with the other person's endpoint, key and project id.

## 2. Create the project (customvision.ai)

Pick the training resource, then **New Project**:

| Setting | Object detection | Classification |
|---|---|---|
| Project type | Object Detection | Classification, **Multilabel** |
| Domain | General [A1] | General [A2] |
| Script mode | *(default)* | `--mode classification` |

Then, via the **gear icon** of the project, copy:
- **Project Id** (left)
- **Endpoint** and **Key** of the *training* resource (right, not the `-Prediction` resource)

Never paste the key in a chat, a file or a commit.

## 3. Upload (PowerShell)

Open a new PowerShell window. The variables only live in that window.

```powershell
$env:CUSTOMVISION_TRAINING_ENDPOINT = "https://<your-resource>.cognitiveservices.azure.com/"
$env:CUSTOMVISION_TRAINING_KEY = "<training key>"
$env:CUSTOMVISION_PROJECT_ID = "<project id>"

$script = "<repo>\tools\customvision\upload_cardd.py"
$cardd = "<downloads>\CarDD\CarDD_release\CarDD_COCO"
```

Optional dry run (shows the selection, uploads nothing, needs no Azure settings):

```powershell
python $script --cardd-dir $cardd --per-tag 100 --dry-run
```

Object detection project:

```powershell
python $script --cardd-dir $cardd --per-tag 100
```

Classification project:

```powershell
python $script --cardd-dir $cardd --per-tag 100 --mode classification
```

Expected output: a `Project:` line with the right name and type, `Created tag ...` six times, a counter up to `352/352`, and `Done: {'OK': 352}`. `OKDuplicate` means the images were already in that project. If the mode does not match the project type, the script stops with "Nothing uploaded".

Unseen test photos for **Quick Test** (copies 15 images from the test split):

```powershell
python $script --cardd-dir $cardd --export-test 15 --test-dir "<downloads>\CarDD\quick-test"
```

## 4. Train and publish

1. customvision.ai → project → **Train** → **Quick Training**.
2. Check the status (works for any project, `trainingTimeInMinutes` stays 0 until it is finished):

   ```powershell
   (Invoke-RestMethod -Headers @{ "Training-Key" = $env:CUSTOMVISION_TRAINING_KEY } -Uri "$($env:CUSTOMVISION_TRAINING_ENDPOINT)customvision/v3.3/training/projects/$($env:CUSTOMVISION_PROJECT_ID)/iterations") | Select-Object name, status, trainingTimeInMinutes
   ```

3. When the status is `Completed`: **Performance** → **Publish** → name `dentify-v1` → the `-Prediction` resource.
4. Put the prediction settings in the API user-secrets (see the Azure section in `CLAUDE.md`):

   ```powershell
   $p = "<repo>\Backend\Dentify.Backend\Dentify.Api"
   dotnet user-secrets set "Azure:CustomVision:PredictionEndpoint" "<prediction endpoint>" --project $p
   dotnet user-secrets set "Azure:CustomVision:PredictionKey" "<prediction key>" --project $p
   dotnet user-secrets set "Azure:CustomVision:ProjectId" "<project id>" --project $p
   dotnet user-secrets set "Azure:CustomVision:PublishedName" "dentify-v1" --project $p
   dotnet user-secrets set "Azure:CustomVision:ProjectType" "Classification" --project $p
   dotnet user-secrets set "Azure:UseFakeServices" "false" --project $p
   ```

## Tag mapping

| CarDD | Tag | Severity in the app |
|---|---|---|
| scratch | `scratch` | Minor |
| dent | `dent` | Moderate |
| crack | `crack` | Moderate |
| glass shatter | `broken-glass` | Severe |
| lamp broken | `broken-lamp` | Severe |
| tire flat | `flat-tire` | Severe |

## Known issue

In October 2026, every training in **Germany West Central** stayed on "Training" for hours without starting (object detection and classification, new and existing resources). A started training cannot be cancelled or deleted, but it is not billed beyond its budget. Microsoft Q&A has similar reports from other regions. Try a resource in another region if your subscription allows it.
