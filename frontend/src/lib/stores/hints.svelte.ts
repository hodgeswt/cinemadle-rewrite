import { writable, get } from 'svelte/store';
import type { HintsResponseDto } from '$lib/dto';
import { getHints, getHintsAnon, getHintsCustomGame } from '$lib/middleware';
import Logger from '$lib/logger';

export type HintsStore = {
    hints: HintsResponseDto | null;
    loading: boolean;
    error: string | null;
};

const HINTS_STORAGE_KEY = 'cinemadle_hints';
const HINTS_DATE_KEY = 'cinemadle_hints_date';

function getStoredHints(): HintsResponseDto | null {
    if (typeof window === 'undefined') return null;
    
    try {
        const storedDate = localStorage.getItem(HINTS_DATE_KEY);
        const today = new Date().toISOString().split('T')[0];
        
        // Clear if different day
        if (storedDate !== today) {
            localStorage.removeItem(HINTS_STORAGE_KEY);
            localStorage.removeItem(HINTS_DATE_KEY);
            return null;
        }
        
        const stored = localStorage.getItem(HINTS_STORAGE_KEY);
        if (stored) {
            return JSON.parse(stored);
        }
    } catch (e) {
        Logger.log('hintsStore: failed to get stored hints: {0}', e);
    }
    return null;
}

function storeHints(hints: HintsResponseDto): void {
    if (typeof window === 'undefined') return;
    
    try {
        const today = new Date().toISOString().split('T')[0];
        localStorage.setItem(HINTS_STORAGE_KEY, JSON.stringify(hints));
        localStorage.setItem(HINTS_DATE_KEY, today);
    } catch (e) {
        Logger.log('hintsStore: failed to store hints: {0}', e);
    }
}

function createHintsStore() {
    const { subscribe, set, update } = writable<HintsStore>({
        hints: getStoredHints(),
        loading: false,
        error: null,
    });

    return {
        subscribe,
        
        async fetchHints(userToken: string, customGameId?: string): Promise<void> {
            update(s => ({ ...s, loading: true, error: null }));
            
            try {
                const result = customGameId 
                    ? await getHintsCustomGame(customGameId, userToken)
                    : await getHints(userToken);
                
                if (result.ok && result.data) {
                    const hints = result.data;
                    storeHints(hints);
                    set({ hints, loading: false, error: null });
                } else {
                    update(s => ({ ...s, loading: false, error: result.error ?? 'Failed to fetch hints' }));
                }
            } catch (e) {
                Logger.log('hintsStore: error fetching hints: {0}', e);
                update(s => ({ ...s, loading: false, error: 'Failed to fetch hints' }));
            }
        },
        
        async fetchHintsAnon(anonUserId: string): Promise<void> {
            update(s => ({ ...s, loading: true, error: null }));
            
            try {
                const result = await getHintsAnon(anonUserId);
                
                if (result.ok && result.data) {
                    const hints = result.data;
                    storeHints(hints);
                    set({ hints, loading: false, error: null });
                } else {
                    update(s => ({ ...s, loading: false, error: result.error ?? 'Failed to fetch hints' }));
                }
            } catch (e) {
                Logger.log('hintsStore: error fetching anon hints: {0}', e);
                update(s => ({ ...s, loading: false, error: 'Failed to fetch hints' }));
            }
        },
        
        invalidate(): void {
            if (typeof window !== 'undefined') {
                localStorage.removeItem(HINTS_STORAGE_KEY);
                localStorage.removeItem(HINTS_DATE_KEY);
            }
            set({ hints: null, loading: false, error: null });
        },
        
        getHintsForField(fieldKey: string): HintsResponseDto[string] | null {
            const state = get({ subscribe });
            return state.hints?.[fieldKey] ?? null;
        }
    };
}

export const hintsStore = createHintsStore();
