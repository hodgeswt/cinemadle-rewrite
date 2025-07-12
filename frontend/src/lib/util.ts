export function hasValue(obj: any): boolean {
    return obj !== null && obj !== undefined;
}

export function isDictionary(obj: any, keyType: string, valueType: string, valueIsArray: boolean): boolean {
    if (!hasValue(obj)) {
        return false;
    }

    if (typeof obj !== 'object' || obj === null) {
        return false;
    }

    for (const key in obj) {
        if (!valueIsArray && typeof obj[key] !== 'string') {
            return false;
        } else if (valueIsArray && !isArray(obj[key], valueType)) {
            return false;
        }
    }

    return true;
}

export function isArray(obj: any, type: string): boolean {
    if (!hasValue(obj)) {
        return false;
    }

    if (!Array.isArray(obj)) {
        return false;
    }

    return obj.every(
        (element: any) => hasValue(element) && typeof element === type,
    );
}

export function isoDateNoTime(): string {

    const date = new Date();
    const options: Intl.DateTimeFormatOptions = {
        timeZone: 'America/New_York',
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
    };

    const formatter = new Intl.DateTimeFormat('en-US', options);
    const formattedParts = formatter.formatToParts(date);

    const year = formattedParts.find(part => part.type === 'year')?.value;
    const month = formattedParts.find(part => part.type === 'month')?.value;
    const day = formattedParts.find(part => part.type === 'day')?.value;

    return `${year}-${month}-${day}`;
}

