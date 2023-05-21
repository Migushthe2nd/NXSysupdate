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

## Example docker-compose.yml

```yaml
version: '3.7'

services:
  nxsysupdate:
    image: 'ghcr.io/migushthe2nd/nxsysupdate:main'
    deploy:
      placement:
        constraints:
          - node.hostname==kaasufo
    restart: unless-stopped
    volumes:
      - '/mnt/user/appdata/nxsysupdate/downloads:/downloads'
      - '/mnt/user/appdata/nxsysupdate:/data'
      - '/mnt/user/appdata/nxsysupdate/prod.keys:/prod.keys'
    environment:
      - 'WEBHOOK_URL=https://discord.com/example'
      - 'DOWNLOAD_URL_BASE=https://example.com/'
      - CHECK_INTERVAL=5
```