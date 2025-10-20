export const prerender = false;
export const ssr = false;
export const csr = true;

export function load({ params }) {
    return {
        id: params.id
    };
}
