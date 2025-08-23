import { get } from 'svelte/store';
import { userStore } from "$lib/stores";
import { type IGuessService } from './IGuessService';
import { GuessService } from './GuessService.svelte';
import { AnonGuessService } from "./AnonGuessService";
import { PurchasesService } from './PurchasesService.svelte';
import { FeatureFlagService } from './FeatureFlagService';
import Logger from '$lib/logger';

export class Container {
    private static _it: Container | null;

    private authGuessService: IGuessService;
    private anonGuessService: IGuessService;
    private purchasesService: PurchasesService;
    private featureFlagService: FeatureFlagService;

    private constructor() {
        Logger.log("+Container.ctor");
        this.authGuessService = new GuessService();
        this.authGuessService.initialize();

        this.anonGuessService = new AnonGuessService();
        this.anonGuessService.initialize();

        this.purchasesService = new PurchasesService();
        this.featureFlagService = new FeatureFlagService();
        this.featureFlagService.initialize();
        Logger.log("-Container.ctor");
    }

    public get FeatureFlagService(): FeatureFlagService {
        return this.featureFlagService;
    }

    public get PurchasesService(): PurchasesService {
        return this.purchasesService;
    }

    public get GuessService(): IGuessService {
        if (get(userStore).loggedIn) {
            return this.authGuessService;
        }
        
        return this.anonGuessService;
    }

    public static it(): Container {
        if (Container._it === undefined || Container._it === null) {
            Container._it = new Container();
        }

        return Container._it;
    }
}