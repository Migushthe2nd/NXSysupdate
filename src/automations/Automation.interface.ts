import Discord from "discord.js";

export interface AutomationInterface {

    name: string;
    shortname: string;

    run(ncaDir: string, saveDir: string, downloadDir: string, masterKey: string): Promise<void | Discord.RestOrArray<Discord.APIEmbedField>>;

}