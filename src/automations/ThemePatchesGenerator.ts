/**
 * This automation will generate Qlaunch Lockscreen ips patches for a firmware
 */
export class ThemePatchesGenerator implements Automation {
    run(ncaDir: string, masterKey: string): Promise<void> {
        return new Promise((resolve, reject) => {
            // run gibkey
            // const ls = spawn("dotnet", ["run", tmpDirDownload], {cwd: gibkeyPath});
            //
            // ls.stderr.on('data', (data) => {
            //     console.error('[gib] ' + data.toString());
            // });
            //
            // ls.stdout.on('data', (data) => {
            //     console.log('[gib] ' + data.toString());
            // });
            //
            // ls.stdout.once('close', () => {
            //     tmpDir.removeCallback();
            // });
        })
    }

}