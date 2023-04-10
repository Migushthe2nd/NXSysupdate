import { downloadLocation, yuiPath, gibkeyPath } from './config';
import SysUpdateScheduler from './SysUpdateScheduler';
import Discord from 'discord.js';
import { changelogEmbed, failedDownloadUpdateEmbed, updateEmbed, updateRemovedEmbed } from './webhookMessages';
import path from 'path';

const hooks = [{ id: process.env.WEBHOOK_ID, token: process.env.WEBHOOK_TOKEN }].map(
	({ id, token }) => new Discord.WebhookClient(id, token)
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
	yuiPath,
	gibkeyPath,
	keysetPath: process.env.KEYFILE_PATH,
	checkFrequency: process.env.CHECK_INTERVAL,
});

scheduler.on('start', () => {
	console.log('Scheduler service started!');
});

scheduler.on('update', async ({ version, versionString, buildNumber }) => {
	try {
		console.log('Update Found, initiating download');

		const downloadDir = path.join(downloadLocation, `${versionString}-${version}-bn_${buildNumber}`);
		const { fileName, md5 } = await scheduler.handler.downloadLatest(downloadDir);

		const embed = updateEmbed({
			version,
			versionString,
			buildNumber,
			downloadUrl: downloadUrlBase + fileName,
			fileMd5: md5,
		});
		sendEmbeds([embed]).then();
		// completePending(versionString, embed, null);
	} catch (e) {
		console.error(e);
		const embed = failedDownloadUpdateEmbed({ version, versionString, buildNumber });
		sendEmbeds([embed]).then();
	}
});

scheduler.on('updateRemoved', ({ version, versionString, buildNumber }) => {
	console.log('Update Removed');

	const embed = updateRemovedEmbed({ version, versionString, buildNumber });
	sendEmbeds([embed]).then();
});

scheduler.on('changelogUpdate', ({ versionString, changelog }) => {
	console.log('Changelog was updated');

	const embed = changelogEmbed({ versionString, changelog });
	sendEmbeds([embed]).then();
	// completePending(versionString, null, embed);
});

scheduler.start();
