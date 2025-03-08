export function hasValue(obj: any): boolean {
  return obj !== null && obj !== undefined;
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
