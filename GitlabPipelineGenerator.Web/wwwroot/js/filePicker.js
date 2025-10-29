export async function showSaveFilePicker(suggestedName) {
    if ('showSaveFilePicker' in window) {
        try {
            const fileHandle = await window.showSaveFilePicker({
                suggestedName: suggestedName,
                types: [{
                    description: 'JSON files',
                    accept: { 'application/json': ['.json'] }
                }]
            });
            return fileHandle;
        } catch (err) {
            if (err.name !== 'AbortError') {
                console.error('Error showing save file picker:', err);
            }
            return null;
        }
    } else {
        // Fallback for browsers that don't support File System Access API
        console.warn('File System Access API not supported');
        return null;
    }
}

export async function writeToFile(fileHandle, content) {
    if (fileHandle) {
        try {
            const writable = await fileHandle.createWritable();
            await writable.write(content);
            await writable.close();
            return true;
        } catch (err) {
            console.error('Error writing to file:', err);
            return false;
        }
    }
    return false;
}