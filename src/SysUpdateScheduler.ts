import { EventEmitter } from 'events';
import SysUpdateHandler from './SysUpdateHandler';
import editJsonFile from 'edit-json-file';
import config from "config";

const dataPath = config.get("dataPath") as string;
const data = editJsonFile(dataPath + "/data.json", { autosave: false });

export default class SysUpdateScheduler extends EventEmitter {
	handler;

	checkInterval;
	checkFrequency;

	/**
	 *
	 * @param frequency in minutes
	 * @param yuiPath the yui executable path
	 * @param certPath the nx_tls_client_cert.pem path
	 * @param keysetPath the prod.keys path
	 */
	constructor({
		checkFrequency = 10,
	}) {
		super();
		this.checkFrequency = checkFrequency;
		this.handler = new SysUpdateHandler();
	}

	private _error(error) {
		this.emit('error', error);
	}

	start() {
		this.checkInterval = setInterval(async () => {
			this._checkChangelog().then();
			this._checkUpdate().then();
		}, 1000 * 60 * this.checkFrequency);

		this.emit('start');

		this._checkChangelog().then();
		this._checkUpdate().then();
	}

	private async _checkUpdate() {
		try {
			const updateData = await this.handler.fetchLatestInfo();
			data.set('lastSuccessfulCheck', new Date());

			const previousVersion = data.get('latestUpdate.version');
			if (updateData.version > previousVersion || !previousVersion) {
				// update
				this.emit('update', updateData);
				data.set('latestUpdate', updateData);
			} else if (updateData.version < previousVersion) {
				// update removed
				this.emit('updateRemoved', previousVersion);
				data.set('latestUpdate', updateData);
			}
		} catch (e) {
			this._error(e);
		}
	}

	private async _checkChangelog() {
		try {
			const changelogData = await this.handler.fetchLatestChangelog();

			const previousVersionString = data.get('latestChangelog.versionString');
			if (changelogData.versionString > previousVersionString || !previousVersionString) {
				// update
				this.emit('changelogUpdate', changelogData);
				data.set('latestChangelog', changelogData);
			}
		} catch (e) {
			this._error(e);
		}
	}
}
