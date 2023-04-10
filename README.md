# NXSysUpdate

- Automatically checks if there is a new system update available for the Nintendo Switch
- Posts to a discord webhook with a download url
- Extracts the master key from mariko package1


## Required docker setup

- Mount a path to `/downloads` for FW files
- env `DOWNLOAD_URL_BASE=https://example.com/`
- env `KEYFILE_PATH=prod.keys`
- env `CHECK_INTERVAL=5` in minutes