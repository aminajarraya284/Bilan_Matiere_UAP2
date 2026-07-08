import { storage } from '@/utils/storage';

const triggerDownload = (content: string, filename: string, contentType: string) => {
  const blob = new Blob([new Uint8Array([0xef, 0xbb, 0xbf]), content], { type: contentType });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
};

export const reportExporter = {
  exportExcel(reportId: number, title: string): boolean {
    try {
      let csvContent = '';
      const filename = `${title.toLowerCase().replace(/\s+/g, '_')}_${new Date().toISOString().split('T')[0]}.csv`;

      if (reportId === 1) {
        const bilans = storage.getBilans();
        csvContent = [
          'Période;Code Matière;Encours Début (kg);Sorties X3 (kg);Total Entrées (kg);Encours Fin Réel (kg);Déchets Fonderie (kg);Réceptions Usinage 2 (kg);Poids non réceptionné (kg);Pièces non réceptionnées;Total Sorties (kg);Écart (kg);Taux Écart (%);Statut;Justification;Date Calcul'
        ]
          .concat(
            bilans.map(
              (b) =>
                `"${b.periode}";"${b.typeMatiere}";${b.encoursDebut.toFixed(2).replace('.', ',')};${b.sortiesX3.toFixed(2).replace('.', ',')};${b.inputTotal.toFixed(2).replace('.', ',')};${b.encoursFinReel.toFixed(2).replace('.', ',')};${b.dechetsFonderie.toFixed(2).replace('.', ',')};${b.receptionsUsinage2.toFixed(2).replace('.', ',')};${b.poidsNonReceptionne.toFixed(2).replace('.', ',')};${(b.nombrePiecesNonReceptionnees || 0).toString()};${b.outputTotal.toFixed(2).replace('.', ',')};${b.ecartKg.toFixed(2).replace('.', ',')};${b.tauxEcart.toFixed(2).replace('.', ',')};"${b.statut}";"${b.justification || ''}";"${b.dateCalcul || ''}"`
            )
          )
          .join('\n');
      } else if (reportId === 2) {
        const dechets = storage.getDechets();
        csvContent = [
          'ID;Date Déclaration;Semaine;Code Article;Désignation;Code Type Matière;Quantité (kg);Type Déchet;Commentaire;Statut;Contremaître'
        ]
          .concat(
            dechets.map(
              (d) =>
                `${d.id};"${d.dateDeclaration}";${d.semaine};"${d.codeArticle}";"${d.designationArticle}";"${d.codeTypeMatiere}";${d.quantiteKg.toFixed(2).replace('.', ',')};"${d.typeDechet || ''}";"${d.commentaire || ''}";"${d.statut}";"${d.utilisateur}"`
            )
          )
          .join('\n');
      } else if (reportId === 4) {
        const sorties = storage.getSortiesX3();
        csvContent = [
          'ID;Date Imputation;Code Matière;Désignation;Quantité Importée (kg);Quantité Utilisée (kg);Transaction;Lot;Pièce Origine;Désignation Origine;Emplacement;Statut;Source;Utilisateur'
        ]
          .concat(
            sorties.map(
              (s) =>
                `${s.id};"${s.dateImputation}";"${s.codeMatiere}";"${s.designation}";${s.quantiteImportee.toFixed(2).replace('.', ',')};${s.quantiteUtilisee.toFixed(2).replace('.', ',')};"${s.transaction}";"${s.lot || ''}";"${s.pieceOrigine || ''}";"${s.designationOrigine || ''}";"${s.emplacement || ''}";"${s.statut}";"${s.source}";"${s.utilisateur}"`
            )
          )
          .join('\n');
      } else if (reportId === 5) {
        const conteneurs = storage.getConteneurs();
        const categories = storage.getCategories();
        const fours = storage.getFours();
        const lines = [
          "Type d'Enregistrement;Identifiant / Catégorie;Date/Période;Code Article / Commentaire;Désignation;Type Matière;Quantité Réelle;Statut;Observation;Inclus bilan fonderie;Visible rapport inventaire"
        ];

        conteneurs.forEach((c) => {
          lines.push(
            `"CONTENEUR";"${c.numeroConteneur}";"${c.dateInventaire}";"${c.codeArticle}";"${c.designationArticle}";"${c.codeTypeMatiere}";${c.quantiteReelle.toFixed(2).replace('.', ',')};"${c.statut}";"${c.observation || ''}";"Oui";"Oui"`
          );
        });

        categories.forEach((cat) => {
          lines.push(
            `"CATEGORIE INCLUSE BILAN";"${cat.categorie}";"${cat.annee}-${cat.mois.toString().padStart(2, '0')}";"${cat.commentaire || ''}";"";"${cat.codeTypeMatiere}";${cat.quantiteReelle.toFixed(2).replace('.', ',')};"${cat.statut}";"";"Oui";"Oui"`
          );
        });

        fours.forEach((item) => {
          lines.push(
            `"FOUR HORS BILAN";"${item.four}";"${item.dateInventaire}";"${item.observation || ''}";"";"${item.codeTypeMatiere}";${item.quantite.toFixed(2).replace('.', ',')};"${item.statut}";"${item.observation || ''}";"Non";"Oui"`
          );
        });

        csvContent = lines.join('\n');
      }

      if (!csvContent) return false;
      triggerDownload(csvContent, filename, 'text/csv;charset=utf-8;');
      return true;
    } catch (e) {
      console.error('Failed to export Excel report', e);
      return false;
    }
  },

  exportPDF(reportId: number, title: string): boolean {
    try {
      let txtContent = '';
      const filename = `${title.toLowerCase().replace(/\s+/g, '_')}_${new Date().toISOString().split('T')[0]}.txt`;
      const separator = '='.repeat(80);
      const subSeparator = '-'.repeat(80);

      if (reportId === 1) {
        const bilans = storage.getBilans();
        txtContent = [
          separator,
          `             RAPPORT BILAN MATIERE MENSUEL - SOPAL UAP2`,
          `             Généré le: ${new Date().toLocaleDateString('fr-FR')} à ${new Date().toLocaleTimeString('fr-FR')}`,
          separator,
          '',
          ...bilans.flatMap((b) => [
            `MATIERE: ${b.typeMatiere}`,
            `Période: ${b.periode}  |  Statut: ${b.statut.toUpperCase()}`,
            subSeparator,
            `  [+] Entrées (INPUT):`,
            `      - Encours Début : ${b.encoursDebut.toLocaleString('fr-FR')} kg`,
            `      - Sorties ERP X3: ${b.sortiesX3.toLocaleString('fr-FR')} kg`,
            `      * TOTAL ENTRÉES : ${b.inputTotal.toLocaleString('fr-FR')} kg`,
            '',
            `  [-] Sorties (OUTPUT):`,
            `      - Encours Fin Réel (Inventaire): ${b.encoursFinReel.toLocaleString('fr-FR')} kg`,
            `      - Déchets Fonderie             : ${b.dechetsFonderie.toLocaleString('fr-FR')} kg`,
            `      - Réceptions Usinage 2         : ${b.receptionsUsinage2.toLocaleString('fr-FR')} kg`,
            `      - Poids non réceptionné        : ${b.poidsNonReceptionne.toLocaleString('fr-FR')} kg (${(b.nombrePiecesNonReceptionnees || 0).toLocaleString('fr-FR')} pièces)`,
            `      * TOTAL SORTIES                : ${b.outputTotal.toLocaleString('fr-FR')} kg`,
            '',
            `  [*] ÉCART CONSTATÉ:`,
            `      - Écart physique (kg)          : ${b.ecartKg.toLocaleString('fr-FR')} kg`,
            `      - Taux d'écart (%)             : ${b.tauxEcart.toFixed(2)} %`,
            b.justification ? `      - Justification                : ${b.justification}` : '',
            subSeparator,
            ''
          ]),
          separator,
          'SOPAL Bilan Matière UAP2 - Direction de Production Fonderie'
        ].join('\n');
      } else if (reportId === 2) {
        const dechets = storage.getDechets();
        txtContent = [
          separator,
          `             RAPPORT DECHETS FONDERIE - SOPAL UAP2`,
          `             Généré le: ${new Date().toLocaleDateString('fr-FR')}`,
          separator,
          '',
          'ID   | Date       | Sem. | Article     | Matière  | Quantité  | Type Déchet          | Commentaire',
          subSeparator,
          ...dechets.map(
            (d) =>
              `${d.id.toString().padEnd(4)} | ${d.dateDeclaration.padEnd(10)} | ${d.semaine.toString().padEnd(4)} | ${d.codeArticle.padEnd(11)} | ${d.codeTypeMatiere.padEnd(8)} | ${d.quantiteKg.toLocaleString('fr-FR').padStart(8)} | ${String(d.typeDechet || '').padEnd(20)} | ${d.commentaire || ''}`
          ),
          subSeparator,
          `Total Déchets Déclarés: ${dechets.reduce((s, d) => s + d.quantiteKg, 0).toLocaleString('fr-FR')} kg`,
          separator
        ].join('\n');
      } else if (reportId === 3) {
        const receptions = storage.getReceptions();
        txtContent = [
          separator,
          `             RAPPORT RECEPTIONS USINAGE 2 - SOPAL UAP2`,
          `             Généré le: ${new Date().toLocaleDateString('fr-FR')}`,
          separator,
          '',
          'N° OF   | Date       | Article     | Matière  | Poids (kg) | Pièces | Atelier',
          subSeparator,
          ...receptions.map(
            (r) =>
              `${r.numOf.padEnd(7)} | ${r.dateReception.padEnd(10)} | ${r.codeArticle.padEnd(11)} | ${r.codeTypeMatiere.padEnd(8)} | ${r.poidsKg.toLocaleString('fr-FR').padStart(10)} | ${r.nombrePieces.toString().padStart(6)} | ${r.atelierOrigine}`
          ),
          subSeparator,
          `Total Réceptions Enregistrées: ${receptions.reduce((s, r) => s + r.poidsKg, 0).toLocaleString('fr-FR')} kg`,
          separator
        ].join('\n');
      } else if (reportId === 5) {
        const conteneurs = storage.getConteneurs();
        const categories = storage.getCategories();
        const fours = storage.getFours();
        const totalConteneurs = conteneurs.reduce((s, c) => s + c.quantiteReelle, 0);
        const totalCategories = categories.reduce((s, c) => s + c.quantiteReelle, 0);
        const totalFours = fours.reduce((s, item) => s + item.quantite, 0);
        const totalRapport = totalConteneurs + totalCategories + totalFours;
        const totalBilan = totalConteneurs + totalCategories;

        txtContent = [
          separator,
          `             RAPPORT INVENTAIRE PHYSIQUE DE FIN DE PERIODE`,
          `             Généré le: ${new Date().toLocaleDateString('fr-FR')}`,
          separator,
          '',
          '--- PARTIE 1 : CONTENEURS ENCOURS ---',
          'Conteneur | Article     | Destination OF | Type Matière | Quantité (kg) | Statut',
          subSeparator,
          ...conteneurs.map(
            (c) =>
              `${c.numeroConteneur.padEnd(9)} | ${c.codeArticle.padEnd(11)} | ${(c.ofDestination || '').padEnd(14)} | ${c.codeTypeMatiere.padEnd(12)} | ${c.quantiteReelle.toLocaleString('fr-FR').padStart(13)} | ${c.statut}`
          ),
          '',
          '--- PARTIE 2 : INVENTAIRE INCLUS DANS BILAN MATIERE FONDERIE ---',
          'Catégorie / Emplacement         | Type Matière | Quantité (kg) | Statut',
          subSeparator,
          ...categories.map(
            (cat) =>
              `${cat.categorie.padEnd(31)} | ${cat.codeTypeMatiere.padEnd(12)} | ${cat.quantiteReelle.toLocaleString('fr-FR').padStart(13)} | ${cat.statut}`
          ),
          '',
          '--- PARTIE 3 : INVENTAIRE FOUR HORS BILAN MATIERE ---',
          'Four                            | Type Matière | Quantité | Statut   | Bilan | Rapport',
          subSeparator,
          ...fours.map(
            (item) =>
              `${item.four.padEnd(31)} | ${item.codeTypeMatiere.padEnd(12)} | ${item.quantite.toLocaleString('fr-FR').padStart(8)} | ${item.statut.padEnd(8)} | Non   | Oui`
          ),
          subSeparator,
          `TOTAL RAPPORT INVENTAIRE: ${totalRapport.toLocaleString('fr-FR')} kg`,
          `TOTAL UTILISE DANS BILAN MATIERE: ${totalBilan.toLocaleString('fr-FR')} kg`,
          `TOTAL INVENTAIRE FOUR HORS BILAN: ${totalFours.toLocaleString('fr-FR')} kg`,
          separator
        ].join('\n');
      }

      if (!txtContent) return false;
      triggerDownload(txtContent, filename, 'text/plain;charset=utf-8;');
      return true;
    } catch (e) {
      console.error('Failed to export PDF report', e);
      return false;
    }
  }
};
