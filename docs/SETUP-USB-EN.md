# Brendigo USB Key — setup with multiple USB sticks (English)

**Alpha 0.2:** This application registers and detects USB drives and can optionally LOCK an active Windows desktop session. It **CANNOT unlock Windows sign-in** using a USB drive.

## Before you start

Use Windows 10 or 11 x64. Check that your regular Windows password or Windows Hello sign-in works. If BitLocker is enabled, securely save its recovery key separately. Prepare at least two physically independent USB flash drives. Download the unsigned portable Windows test build from [GitHub Actions](https://github.com/bren-wp/USB-Key/actions/workflows/windows-build.yml) or from [Releases](https://github.com/bren-wp/USB-Key/releases) when available.

## Register independent USB drives

1. Sign in to Windows normally. Run the Brendigo USB Key application on your computer.
2. Insert USB #1, then select **USB Keys → Add USB**.
3. Select the correct removable drive, enter **Primary USB**, and confirm.
4. The program creates a hidden folder called **.brendigo-usb-key** and a **key.json** file on the USB.
5. Safely remove USB #1. Insert USB #2 and register it separately as **Backup USB**.
6. Repeat with USB #3 (e.g. **Travel USB**) or more physical flash drives.
7. Reinsert each drive separately and verify the correct status.
8. Leave auto-lock disabled until you have confirmed the Windows fallback login.

**Never copy key.json between sticks.** Copying a token duplicates the same identity. Plain USB storage is copyable, so this is not equivalent to hardware FIDO2 authentication.

## Running the portable application from USB

You may keep a copy of the portable EXE on a USB drive and launch it manually **after logging into Windows**. Windows does not automatically run it on the locked sign-in screen. The USB token is data, not a Windows login credential.

## Background monitoring and system tray

Closing the main window hides the app to the Windows notification area; it continues monitoring. Double-click the tray icon to reopen it, or use the menu to lock Windows or exit. Fully exiting stops monitoring. Auto-start launches the app directly in the tray.

## If your USB is damaged or lost

1. Log in with your regular Windows password or Windows Hello.
2. Open Brendigo USB Key and revoke the missing USB.
3. Register a new physical USB and verify that your backup devices still work.

Revocation on the PC does not erase the token file from the USB. The BitLocker recovery key is separate from the Windows account password.

## Troubleshooting

- **USB not listed:** This version only enumerates ready drives that Windows reports as **Removable**. Some external USB hard drives appear as **Fixed** drives and are not supported.
- **Device not recognized:** Check the USB port and token file. Do not reuse cloned tokens.
- **Auto-lock not triggered:** The program must be running and must first have recognized a registered USB in the current session.
- **USB does not unlock Windows:** Expected behavior at this stage. Use Windows password / Hello.

**Brendigo** · https://brendigo.com · MIT License
