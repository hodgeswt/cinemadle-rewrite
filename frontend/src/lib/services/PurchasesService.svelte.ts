import { isPurchaseResponseDto, isQuantitiesDto, type PurchaseDetailsDto, type PurchaseResponseDto, type QuantitiesDto } from "$lib/dto";
import Logger from "$lib/logger";
import { get, post } from "$lib/middleware";
import { err, ok, type Result } from "$lib/result";
import { get as sget } from 'svelte/store';
import { userStore } from "$lib/stores";

export class PurchasesService {

    constructor() { }

    public async purchase(productId: string): Promise<Result<PurchaseResponseDto>> {
        Logger.log("Making purchase request to server");
        let result = await post(
            '/api/payments/purchase',
            true,
            JSON.stringify({ productId: productId, quantity: 1 } as PurchaseDetailsDto),
            { Authorization: sget(userStore).jwt, "Content-Type": "application/json"}
        );

        if (!result.ok) {
            return err("unable to make purchase request");
        }

        const data = JSON.parse(result.data!);
        if (!isPurchaseResponseDto(data)) {
            return err("unable to make purchase request");
        }

        return ok(data);
    }

    public async getQuantities(): Promise<Result<QuantitiesDto>> {
        let result = await get(
            '/api/payments/quantities',
            { },
            { Authorization: sget(userStore).jwt },
            false,
            true,
        )

        if (result.ok) {
            let data = JSON.parse(result.data!);
            if (isQuantitiesDto(data)) {
                return ok(data as QuantitiesDto);
            } 

            return err('bad data')
        } 
        
        return err("unable to load quantities")
    }
}