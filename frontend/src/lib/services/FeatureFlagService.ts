import { isFeatureFlagsDto, type FeatureFlagsDto } from "$lib/dto";
import { get } from "$lib/middleware";

export class FeatureFlagService {
    private featureFlags: {[key: string]: boolean} = {}

    private readonly ttl = 1800 * 1000; // ms

    private lastRead: number = 0;

    public async getFeatureFlag(name: string): Promise<boolean> {
        await this.initialize();
        
        if (name in this.featureFlags) {
            return this.featureFlags[name];
        }

        return false;
    }

    public async initialize() {
        if (this.lastRead !== null && this.lastRead + this.ttl <= Date.now()) {
            return;
        }

        this.lastRead = Date.now();

        const result = await get("featureFlags", null, null);

        if (!result.ok) {
            return;
        }

        const data = JSON.parse(result.data!);

        if (isFeatureFlagsDto(result)) {
            return;
        }

        this.featureFlags = (data as FeatureFlagsDto).featureFlags;
    }
}