export default class Logger {
    private static isDebug = import.meta.env.PROD;

    private static format(template: string, ...args: any[]): string {
        let o = template;
        for (let index = 0; index < args.length; index++) {
            o = o.replace(`{${index}}`, JSON.stringify(args[index]));
        }

        return o;
    }

    public static log(template: string, ...args: any[]): void {
        if (Logger.isDebug) { return; }

        console.log(Logger.format(template, ...args));
    }
}