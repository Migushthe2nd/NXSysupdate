interface Automation {

    run(ncaDir: string, masterKey: string): Promise<void>;

}