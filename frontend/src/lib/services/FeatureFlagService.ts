import { isFeatureFlagsDto, type FeatureFlagsDto } from "$lib/dto";
import Logger from "$lib/logger";
import { get } from "$lib/middleware";

export class FeatureFlagService {
    private featureFlags: {[key: string]: boolean} = {}

    private readonly ttl = 1800 * 1000; // ms

    private lastRead: number = 0;

    public async getFeatureFlag(name: string): Promise<boolean> {
        Logger.log("+FeatureFlagService.get({0})", name)
        await this.initialize();
        
        if (name in this.featureFlags) {
            Logger.log("+FeatureFlagService.get({0}): found {1}", name, this.featureFlags[name])
            return this.featureFlags[name];
        }

        Logger.log("+FeatureFlagService.get({0}): default false", name)
        return false;
    }

    public async initialize() {
        Logger.log("+FeatureFlagService.initialize");
        if (this.lastRead !== 0 && this.lastRead + this.ttl > Date.now()) {
            Logger.log("-FeatureFlagService.initialize: early return");
            Logger.log("-FeatureFlagService.initialize");
            return;
        }

        this.lastRead = Date.now();

        const result = await get("featureFlags", null, null);

        if (!result.ok) {
            Logger.log("FeatureFlagService.initialize: bad response");
            Logger.log("-FeatureFlagService.initialize");
            return;
        }

        const data = JSON.parse(result.data!);

        if (!isFeatureFlagsDto(data)) {
            Logger.log("FeatureFlagService.initialize: bad data: {0}", data);
            Logger.log("-FeatureFlagService.initialize");
            return;
        }

        this.featureFlags = (data as FeatureFlagsDto).featureFlags;

        Logger.log("FeatureFlagService.initialize: got flags {0}", this.featureFlags)
        Logger.log("-FeatureFlagService.initialize");
    }
}