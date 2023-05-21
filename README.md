# NXSysUpdate

- Automatically checks if there is a new system update available for the Nintendo Switch
- Posts to a discord webhook with a download url
- Extracts the master key from mariko package1
- Note that your keys file may be modified with a new master key if one is found


## Required docker setup

- Mount a path to `/downloads` for FW files
- Mount a path to `/data` for data storage
- Mount your keys to `/prod.keys`
- env `WEBHOOK_URL=https://discord.com/example`
- env `DOWNLOAD_URL_BASE=https://example.com/`
- env `CHECK_INTERVAL=5` in minutes