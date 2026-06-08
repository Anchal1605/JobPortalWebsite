export function stripHtml(html: string | null | undefined, maxLength = 160): string {
    if (!html) return '';

    let text = html
        .replace(/<[^>]+>/g, ' ')
        .replace(/&nbsp;/g, ' ')
        .replace(/&amp;/g, '&')
        .replace(/\s+/g, ' ')
        .trim();

    if (maxLength > 0 && text.length > maxLength) {
        return text.slice(0, maxLength).trimEnd() + '…';
    }
    return text;
}
