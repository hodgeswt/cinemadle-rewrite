import Fuse from "fuse.js"

export const find = (input: string, data: string[]): string[] => {
    console.log(`Finding ${input}`)

    const opts = {
        includeScore: false,
        threshold: 0.3,
        distance: 80,
        ignoreDiacritics: true,
        minMatchCharLength: 3,
        keys: ["name"]
    }

    const fuse = new Fuse(data, opts)
    const results = fuse.search(input)

    return results.map(x => x.item)
}
