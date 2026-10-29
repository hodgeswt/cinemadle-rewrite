import Fuse from "fuse.js"

const opts = {
    includeScore: false,
    threshold: 0.3,
    distance: 80,
    ignoreDiacritics: true,
    minMatchCharLength: 1,
    keys: ["name"]
}

const memo: Map<string, string[]> = new Map<string, string[]>();

export const find = (input: string, data: string[], gameId?: string): string[] => {
    const mk = `${input}${gameId}`;
    if (gameId && memo.has(mk)) {
        return memo.get(mk)!;
    } 

    if (input === "" || input === undefined || data === undefined || data.length === 0) {
        return [] as string[]
    }

    const fuse = new Fuse(data, opts)
    const results = fuse.search(input).map(x => x.item)
    memo.set(mk, results);
    
    return results;
}
