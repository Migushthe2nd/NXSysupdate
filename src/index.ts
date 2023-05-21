import config from 'config'
import SysUpdateScheduler from './SysUpdateScheduler';
import Discord from 'discord.js';
import {changelogEmbed, failedDownloadUpdateEmbed, updateEmbed, updateRemovedEmbed} from './webhookMessages';
import path from 'path';

const keysetPath = config.get("keysetPath") as string;
const downloadsLocation = config.get("downloadsLocation") as string;
const yuiPath = config.get("yuiPath") as string;
const gibkeyPath = config.get("gibkeyPath") as string;
const certPath = config.get("certPath") as string;

const hooks = [{ url: process.env.WEBHOOK_URL }].map(
    ({ url }) => new Discord.WebhookClient({url})
);

const sendEmbeds = (embeds) => {
    return new Promise(async (resolve) => {
        for (const hook of hooks) {
            await hook.send({embeds});
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

        const downloadDir = path.join(downloadsLocation, versionString);
        // if path doesn't exist, create it
        if (!require("fs").existsSync(downloadDir)) {
            require("fs").mkdirSync(downloadDir, {recursive: true});
        }
        const {filePath, md5, extraEmbedFields} = await scheduler.handler.downloadLatest(downloadDir, {version, versionString, buildNumber});

        const embed = updateEmbed({
            version,
            versionString,
            buildNumber,
            downloadUrl: path.join(process.env.DOWNLOAD_URL_BASE, versionString, path.basename(filePath)),
            fileMd5: md5,
        });
        embed.addFields(extraEmbedFields)
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
