export function setLocalStorageValue(key: string, value: any) {
    localStorage.setItem(key, JSON.stringify(value));
}

export function getLocalStorageValue(key: string, defaultValue: any) {
    const storedValue = localStorage.getItem(key);
    return storedValue !== null ? JSON.parse(storedValue) : defaultValue;
}