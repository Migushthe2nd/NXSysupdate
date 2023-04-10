import path from 'path';
import { spawn } from 'child_process';
import cheerio from 'cheerio';
import axios from 'axios';
import TurndownService from 'turndown';
import tmp from 'tmp';
import md5File from 'md5-file';
import AdmZip from 'adm-zip';

const turndownService = new TurndownService({ bulletListMarker: '-' });

export default class SysUpdateHandler {
	yuiPath;
	yuiBaseArgs: string[];

	constructor({ certPath, keysetPath, yuiPath, gibkeyPath }) {
		this.gibkeyPath = gibkeyPath;
		this.yuiPath = yuiPath;
		this.yuiBaseArgs = ['-q', '--cert', certPath, '--keyset', keysetPath];
	}

	private static _killProcess(process) {
		if (!process.killed) process.kill();
	}

	fetchLatestInfo() {
		return new Promise<{ version: string; versionString: string; buildNumber: string }>((resolve, reject) => {
			const ls = spawn(this.yuiPath, [...this.yuiBaseArgs, '--info']);

			ls.stderr.on('data', (data) => {
				SysUpdateHandler._killProcess(ls);
				reject('[yui] ' + data.toString());
			});

			ls.stdout.on('data', (data) => {
				const line = data.toString().trim();
				console.log('[yui]', line);

				if (line.includes('Latest version on CDN:')) {
					resolve({
						version: line.match(/\[(.*)]/)[1],
						versionString: line.split(' ')[4],
						buildNumber: line.split('=')[1].trim(),
					});
				}
			});
		});
	}

	fetchLatestChangelog() {
		return new Promise<{ versionString: string; changelog: string }>(async (resolve, reject) => {
			try {
				const res = await axios.get(
					'https://en-americas-support.nintendo.com/app/answers/detail/a_id/22525/kw/nintendo%20switch%20system%20update'
				);

				const $ = cheerio.load(res.data);
				const div = $('.update-versions');

				const version = $('h3', div)
					.text()
					.match(/\d+\.\d+.\d+/)[0];

				const changes = $(div).children(':not(h3)');
				let changelog = '';
				changes.each((i, elem) => {
					const parsed = turndownService.turndown($(elem).html()) + '\n';
					changelog += parsed;
				});

				resolve({
					versionString: version,
					changelog: changelog.replace(/\n\s{4}/gm, '\n\u2800   '),
				});
			} catch (e) {
				reject(e);
			}
		});
	}

	downloadLatest(downloadDir, gibkeyPath) {
		const tmpDir = tmp.dirSync({ unsafeCleanup: true });
		const tmpDirDownload = tmpDir.name;
		let error = true;

		return new Promise<{ filePath: string; fileName: string; md5: string }>((resolve, reject) => {
			const ls = spawn(this.yuiPath, [...this.yuiBaseArgs, '--latest', '--out', tmpDirDownload]);

			ls.stderr.on('data', (data) => {
				SysUpdateHandler._killProcess(ls);
				error = true;
				reject('[yui] ' + data.toString());
			});

			ls.stdout.on('data', (data) => {
				const line = data.toString().trim();
				console.log('[yui]', line);

				if (line.includes('All done')) {
					error = false;
				}
			});

			ls.stdout.on('close', () => {
				if (!error) {
					const outFile = downloadDir + '.zip';

					const zip = new AdmZip();
					zip.addLocalFolder(tmpDirDownload);
					zip.writeZip(outFile);

					// const ls = spawn(this.gibKeyPath, [tmpDirDownload]);
					// TODO: spawn gibkey and pass nca folder

					resolve({ filePath: outFile, fileName: path.basename(outFile), md5: md5File.sync(outFile) });
					console.log('Wrote update file to', outFile);
					tmpDir.removeCallback();
				}
			});
		});
	}
}
