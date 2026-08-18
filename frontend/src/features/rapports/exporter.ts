import type { RapportDownload } from './rapports.types';

export const reportExporter = {
  download({ blob, fileName }: RapportDownload): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  }
};
