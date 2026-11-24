import { FeatureFlagsDto } from "$lib/datamodel/dto.ts";
import Logger from "$lib/logger.ts";
import { get } from "$lib/middleware.ts";
import { Validate } from "../business/typing/validations.ts";

export class FeatureFlagService {
    private featureFlags: Map<string, boolean> = new Map<string, boolean>;

    private readonly ttl = 1800 * 1000; // ms

    private lastRead: number = 0;

    public async getFeatureFlag(name: string): Promise<boolean> {
        Logger.log("+FeatureFlagService.get({0})", name)
        await this.initialize();
        
        if (name in this.featureFlags) {
            Logger.log("+FeatureFlagService.get({0}): found {1}", name, this.featureFlags.get(name))
            return this.featureFlags.get(name) ?? false;
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

        const result = await get("api/flags/all", null, null, undefined, true);

        if (!result.ok) {
            Logger.log("FeatureFlagService.initialize: bad response");
            Logger.log("-FeatureFlagService.initialize");
            return;
        }

        const data = JSON.parse(result.data!);

        if (!Validate(FeatureFlagsDto, data)) {
            Logger.log("FeatureFlagService.initialize: bad data: {0}", data);
            Logger.log("-FeatureFlagService.initialize");
            return;
        }

        this.featureFlags = data.featureFlags;

        Logger.log("FeatureFlagService.initialize: got flags {0}", this.featureFlags)
        Logger.log("-FeatureFlagService.initialize");
    }
}