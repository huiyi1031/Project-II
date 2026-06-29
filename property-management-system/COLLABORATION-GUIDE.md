# GitHub Team Collaboration Guide
**Project:** Property Management System Maintenance Module

This guide explains exactly how Ng Hui Yi, Alyssa Leong, Chong Jin Wei, and Wong One Ee will use GitHub Desktop to collaborate on this project without breaking each other's code.

---

## Phase 1: First-Time Setup (Everyone must do this once)

1. **Accept Invitation**: Hui Yi (the owner) will send an invite to your GitHub account. Check your email and accept it.
2. **Clone the Code**: 
   - Open GitHub Desktop.
   - Click `File` > `Clone repository...`
   - Select `huiyi1031/Project-II` and click **Clone**.
3. **Install Dependencies**:
   - Open Visual Studio Code.
   - Open a terminal (`Terminal` > `New Terminal`).
   - Navigate to the UI folder and install packages:
     ```bash
     cd property-management-ui
     npm install
     ```

---

## Phase 2: The Daily Workflow (Do this every time you code)

Whenever you sit down to work on your specific modules, follow these 3 strict steps:

### Step 1: PULL (Before you write any code)
Always get the latest updates from your teammates before you start.
- Open GitHub Desktop.
- Click the **Fetch origin** button at the top right.
- If the button changes to **Pull origin**, click it! (This downloads your teammates' new code).

### Step 2: CODE (Work on your assigned modules only)
Because you have divided the work perfectly, stick to your folders to avoid conflicts:
- 🔵 **Hui Yi**: `features/manager/units`, `assets`, `proactive`
- 🟢 **Alyssa**: `features/tenant/create-request`, `track-request`, `chat`
- 🟡 **Jin Wei**: `features/technician/*`
- 🔴 **One Ee**: `features/manager/requests`, `work-orders`, `auth/register`

*Test your code in the browser (`npm run dev` or `ng serve`) and make sure it works!*

### Step 3: COMMIT & PUSH (When you finish a feature)
Save your work to GitHub so the rest of the team can get it.
- Open GitHub Desktop.
- In the bottom left corner, type a short **Summary** (e.g., *"Alyssa: Added submit button to create request"*).
- Click the blue **Commit to main** button.
- Finally, click **Push origin** at the top right to upload it to GitHub.

---

## Phase 3: Golden Rules to Prevent Disasters

> [!WARNING]
> **Rule 1: Never touch the `node_modules` folder.** 
> It is excluded from GitHub automatically. If your code won't run, type `npm install` in the terminal.

> [!IMPORTANT]
> **Rule 2: Do not edit the exact same file at the exact same time.**
> If Alyssa and One Ee both edit `app-routing.module.ts` on the same day without pulling first, GitHub will create a "Merge Conflict." If you must edit a shared file (like a main routing file or navigation bar), message your group chat first: *"I am pushing an update to the navbar now, please pull!"*

> [!TIP]
> **Rule 3: Push often.**
> Don't wait until Sunday to push all your code. Push small updates every day. It makes fixing problems much easier!
