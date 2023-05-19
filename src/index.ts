import config from 'config'
import SysUpdateScheduler from './SysUpdateScheduler';
import {WebhooksAPI} from '@discordjs/core';
import {changelogEmbed, failedDownloadUpdateEmbed, updateEmbed, updateRemovedEmbed} from './webhookMessages';
import path from 'path';

const keysetPath = config.get("keysetPath") as string;
const downloadLocation = config.get("downloadLocation") as string;
const yuiPath = config.get("yuiPath") as string;
const gibkeyPath = config.get("gibkeyPath") as string;
const certPath = config.get("certPath") as string;

const hooks = [{id: process.env.WEBHOOK_ID, token: process.env.WEBHOOK_TOKEN}].map(
    ({id, token}) => new WebhooksAPI.WebhookClient(id, token)
);

const sendEmbeds = (embeds) => {
    return new Promise(async (resolve) => {
        for (const hook of hooks) {
            for (const embed of embeds) {
                await hook.send(embed);
            }
        }

        resolve(null);
    });
};

const scheduler = new SysUpdateScheduler({
    checkFrequency: Number(process.env.CHECK_INTERVAL),
});

scheduler.on('start', () => {
    console.log('Scheduler service started!');
});

scheduler.on('update', async ({version, versionString, buildNumber}) => {
    try {
        console.log('Update Found, initiating download');

        const downloadDir = path.join(downloadLocation, `${versionString}-${version}-bn_${buildNumber}`);
        const {fileName, md5} = await scheduler.handler.downloadLatest(downloadDir);

        const embed = updateEmbed({
            version,
            versionString,
            buildNumber,
            downloadUrl: process.env.DOWNLOAD_URL_BASE + fileName,
            fileMd5: md5,
        });
        sendEmbeds([embed]).then();
        // completePending(versionString, embed, null);
    } catch (e) {
        console.error(e);
        const embed = failedDownloadUpdateEmbed({version, versionString, buildNumber});
        sendEmbeds([embed]).then();
    }
});

scheduler.on('updateRemoved', ({version, versionString, buildNumber}) => {
    console.log('Update Removed');

    const embed = updateRemovedEmbed({version, versionString, buildNumber});
    sendEmbeds([embed]).then();
});

scheduler.on('changelogUpdate', ({versionString, changelog}) => {
    console.log('Changelog was updated');

    const embed = changelogEmbed({versionString, changelog});
    sendEmbeds([embed]).then();
    // completePending(versionString, null, embed);
});

scheduler.start();
