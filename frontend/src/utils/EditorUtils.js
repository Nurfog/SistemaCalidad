/**
 * Generates an HTML table based on rows and columns.
 */
export const generateTableHtml = (rows, cols) => {
    let html = '<table style="width:100%; border-collapse: collapse; border: 1px solid #ccc; margin: 10px 0;">';

    // Header
    html += '<thead style="background-color: #f3f4f6;"><tr>';
    for (let c = 0; c < cols; c++) {
        html += `<th style="border: 1px solid #ccc; padding: 8px; text-align: left;">Col ${c + 1}</th>`;
    }
    html += '</tr></thead>';

    // Body
    html += '<tbody>';
    for (let r = 0; r < rows; r++) {
        html += '<tr>';
        for (let c = 0; c < cols; c++) {
            html += '<td style="border: 1px solid #ccc; padding: 8px; min-height: 20px;"> </td>';
        }
        html += '</tr>';
    }
    html += '</tbody></table>';

    return html;
};

/**
 * Parses content for commands like tabla(3,5) and replaces them with HTML.
 * Returns the transformed content and a boolean indicating if changes were made.
 */
export const parseEditorCommands = (content) => {
    const tableRegex = /tabla\((\d+)[,.]\s*(\d+)\)/gi;
    let newContent = content;
    let hasChanged = false;

    newContent = content.replace(tableRegex, (match, rows, cols) => {
        hasChanged = true;
        return generateTableHtml(parseInt(rows), parseInt(cols));
    });

    return { content: newContent, hasChanged };
};
