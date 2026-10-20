# AI.NET – Challenge 1

> Source: the "Challenge 1" slides of the AI.NET course (Thomas More).

## Assignment

Program a cross-platform app with AI: a **.NET MAUI** app, an **Azure AI service** and an **API**.

Teams of 2 students (or alone).

Example topics from the slides:

- Parking App
- Scan Shop
- Fridge Chef
- Fatigue detection for drivers
- Automatic Document Archiver
- …

Be creative! Choose a **realistic** topic where using an AI service in your app truly adds value.
No inspiration? Ask an AI, for example: *"Can you give examples of practical applications where using Azure Face AI would be useful?"*

**Not good:** *"I scan a plant, it gets recognized, and I can mark it as a favorite."*

## Extra requirements

### App

- [ ] Create the app with **.NET MAUI** and the **MVVM Community Toolkit**. The code-behind files stay empty.
- [ ] Make sure the app has an **attractive, user-friendly interface**.
- [ ] The app has **multiple pages**.
- [ ] Use a **Navigation Service**.
- [ ] Send **messages** to pass info from one page to another. There are at least **2 messages**.

### AI

- [ ] Create/train an **Azure AI model** and integrate it with the app.
- [ ] More than one model is allowed (Azure AI + OpenAI).

### API

- [ ] The app is linked to an **API written in .NET**, built the way it was taught last year.
- [ ] The API is used to **read** information and also to **write** data.
- [ ] At least **2 tables** connected by a **one-to-many** relationship. Both tables are used in the app.
- [ ] Use the **Layered Architecture**.
- [ ] Use the **Repository Design Pattern**.
- [ ] Work **asynchronously**.
- [ ] Host the API and the database **locally**.
- [ ] Use **Ngrok** (or another tool) to expose the local API to the internet.

### General

- [ ] Be creative: don't just mimic the **SafariSnap** app from the lessons. Replacing "Animal" with "Plant" everywhere is not enough.
- [ ] Respect the **naming conventions** and **coding guidelines** from the lessons. This includes the naming and structure of folders, classes and other elements.
