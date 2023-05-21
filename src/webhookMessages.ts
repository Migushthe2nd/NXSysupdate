import {MessageEmbed} from 'discord.js';
import path from "path";
import {VersionInfo} from "./index";

const defaultEmbed = () => {
    return new MessageEmbed().setTimestamp();
};

// Totally not stolen from https://github.com/discordjs/discord.js/blob/44ac5fe6dfbab21bb4c16ef580d1101167fd15fd/src/util/Util.js#L65-L80
const _splitMessage = (text: string, {maxLength = 1000, char = '\n', prepend = '', append = ''} = {}) => {
    if (!text) return;
    if (text.length <= maxLength) return [text];
    const splitText = text.split(char);
    if (splitText.some((chunk) => chunk.length > maxLength)) throw new RangeError('SPLIT_MAX_LEN');
    const messages = [];
    let msg = '';
    for (const chunk of splitText) {
        if (msg && (msg + char + chunk + append).length > maxLength) {
            messages.push(msg + append);
            msg = prepend;
        }
        msg += (msg && msg !== prepend ? char : '') + chunk;
    }
    return messages.concat(msg).filter((m) => m);
};

const _addChangelogFields = (embed: MessageEmbed, text: string) => {
    const messages = _splitMessage(text);

    if (messages) {
        for (let i = 0; i < messages.length; i++) {
            embed.addFields([
                {name: '\u2800', value: messages[i]},
            ]);
        }
    }
};

export const updateEmbed = ({version, versionString, buildNumber, downloadUrl, fileMd5}: VersionInfo & {downloadUrl: string, fileMd5: string}) => {
    return defaultEmbed()
        .setTitle(`New CDN Firmware: ${versionString}`)
        .addFields([
            {name: 'Version Number', value: version},
            {name: 'Build Number', value: buildNumber},
            {name: 'MD5', value: fileMd5},
            {name: 'Download', value: `[${path.basename(downloadUrl)}](${downloadUrl})`},
        ])
        .setColor('GREEN')
        .setFooter({text: 'Powered by yui'});
};

export const failedDownloadUpdateEmbed = ({version, versionString, buildNumber}: VersionInfo) => {
    return defaultEmbed()
        .setTitle(`New CDN Firmware: ${versionString}`)
        .addFields([
            {name: 'Version Number', value: version},
            {name: 'Build Number', value: buildNumber},
            {name: 'Download', value: '*Could not download firmware*'},
        ])
        .setColor('GREEN')
        .setFooter({text: 'Powered by yui'});
};

export const pendingUpdateEmbed = ({versionString}: VersionInfo) => {
    return defaultEmbed()
        .setTitle(`New CDN Firmware: ${versionString}`)
        .setDescription('*Check back again later for more details*')
        .setColor('GREEN')
        .setFooter({text: 'Powered by yui'});
};

export const updateRemovedEmbed = ({version, versionString, buildNumber}: VersionInfo) => {
    return defaultEmbed()
        .setTitle(`Firmware Removed: ${versionString}`)
        .addFields([
            {name: 'Version Number', value: version},
            {name: 'Build Number', value: buildNumber},
        ])
        .setColor('RED')
        .setFooter({text: 'Powered by yui'});
};

export const changelogEmbed = ({versionString, changelog}: {versionString: string, changelog: string}) => {
    const embed = defaultEmbed()
        .setTitle(`New Firmware Changelog: ${versionString}`)
        .setColor('DARK_GREY');

    _addChangelogFields(embed, changelog);

    return embed;
};

export const pendingChangelogEmbed = ({versionString}: VersionInfo) => {
    return defaultEmbed()
        .setTitle(`Firmware Changelog: ${versionString}`)
        .setDescription('*Check back again later*')
        .setColor('DARK_GREY');
};
