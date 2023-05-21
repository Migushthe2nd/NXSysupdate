import {spawn} from "child_process";
import config from "config";
import path from "path";
import {EmbedField} from "discord.js";
import {AutomationInterface} from "./Automation.interface";

const keysetPath = config.get("keysetPath") as string;
const qpatcherPath = config.get("qpatcherPath") as string;
const downloadsLocation = config.get("downloadsLocation") as string;


/**
 * This automation will generate Qlaunch Lockscreen ips patches for a firmware
 */
export class ThemePatchesGenerator implements AutomationInterface {
    name = "QLaunch Lockscreen Patcher";
    shortname = "qpatcher";

    run(ncaDir: string, saveDir: string, downloadLinkDir: string, masterKey: string): Promise<void | EmbedField[]> {

        return new Promise((resolve) => {
            const ls = spawn("dotnet", ["run", keysetPath, ncaDir, saveDir], {cwd: qpatcherPath});

            let fileName: string | null = null;
            ls.stderr.on('data', (data) => {
                console.error('[qpatcher]', data.toString());
            });

            ls.stdout.on('data', (data) => {
                console.log('[qpatcher]', data.toString());
                const match = data.toString().match(/Saved as: (.*)/);
                if (match) {
                    fileName = path.basename(match[1]);
                }
            });

            ls.stdout.once('close', () => {
                if (!fileName) {
                    resolve([{
                        name: "QLaunch Patcher", value: `*⚠️ Failed to generate*`
                    }] as EmbedField[]);
                } else {
                    resolve([{
                        name: "QLaunch Patcher", value: `[${fileName}](${path.join(downloadLinkDir, fileName)})`
                    }] as EmbedField[]);
                }
            });
        })
    }

}