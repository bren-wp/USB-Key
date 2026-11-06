# Brendigo USB Key — initial password and recovery

## First use
The one-time starting password for the app management interface is **Brendigo**. Enter it on first setup, then immediately replace it with your own private password of at least **12 characters**. The shared initial word cannot open the management interface after configuration.

This is **not your Windows password**, Windows Hello PIN, BitLocker recovery key or USB hardware credential. USB insertion does not currently unlock Windows sign-in.

## Routine access
Enter the private password when opening the dashboard manually or from the Windows system tray. Closing the window locks the management UI again, while USB monitoring may continue in the background. Five incorrect attempts cause a temporary in-process delay; this is not equivalent to Windows operating-system authentication.

## Password changes
Open **Settings → Change application password** in the authenticated app. Enter your current password, a new password of 12–128 characters and its confirmation. Once the update succeeds, the old password is revoked. Password verification uses PBKDF2-SHA256 with a random salt, and the local record is additionally protected with Windows DPAPI CurrentUser.

## Lost USB
Sign in to Windows normally with password or Hello, then open Brendigo USB Key, revoke the lost USB and enroll a separate physical replacement.

## Forgotten app password
Your normal Windows login is unchanged. There is no permanent universal backup password. The local app password record is in LocalAppData/Brendigo/USBKey/access.dat, separate from the USB registration database registry.dat. Back up your data before recovery changes. **Do not delete registry.dat** to reset an application password.

A user or malicious process that can already modify files under the same Windows profile can circumvent this local UI-only barrier. It is not a substitute for Windows or hardware authentication.

Author: Brendigo · https://brendigo.com · MIT.
