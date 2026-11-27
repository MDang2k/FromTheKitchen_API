# 5. Store uploaded images in a configurable folder

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

Admins upload a picture with each menu item. Originally the files went into `wwwroot/images`, inside the deployed app. On Azure App Service every deploy replaces that folder, so uploads would vanish, or the write would fail when the app runs from a read-only package.

## Decision

- **Upload folder from config:** uploads are saved to the folder in `ImageStorage:Path`, defaulting to `wwwroot/images`. On App Service it's set to `/home/data/images`, which is persistent storage that survives restarts and redeploys.
- **Two folders behind one URL:** the app serves that folder at `/images`, next to the seeded images that ship in `wwwroot/images`.
- **The client's file name is never used.** Each upload is saved as a random `<guid>.<ext>`, so a name like `../../x.jpg` can't write outside the folder, and two uploads can't overwrite each other.
- **Only images are accepted:** `.jpg`, `.jpeg`, `.png`, `.webp` and `.gif`, up to 5 MB.
- **Old files are cleaned up only after a successful save.** When an item's image is replaced or the item is deleted, the old file is removed after the database save succeeds, so a failed save never loses the image. Create and update share one `SaveImageAsync` helper.

## Consequences

- **No new dependency or cloud service.** Local development and the tests work as before (the tests point `ImageStorage:Path` at a temp folder).
- **Works across instances.** `/home` is shared by every instance of one App Service app.
- **No CDN and no image resizing.** Images are served by the app itself.
- **The extension is the only check.** A non-image renamed to `.jpg` gets through. That's acceptable because only Admins can upload.
- **Upgrade path:** Azure Blob Storage, behind the same save and delete calls, if images need a CDN or must outlive the App Service app.
