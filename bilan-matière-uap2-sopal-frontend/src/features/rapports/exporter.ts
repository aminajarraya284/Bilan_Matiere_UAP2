import { BUSINESS_RULES } from '@/config/businessRules';
import { testDataRepository } from '@/services/testDataRepository.service';
import { calculateBilanFonderie } from '@/services/bilanCalculation.service';

const centralInventoryViews = () => {
  const dataset = testDataRepository.getDataset();
  return {
    conteneurs: dataset.inventaireConteneurs.map((item) => ({
      id: item.id,
      dateInventaire: item.dateInventaire,
      numeroConteneur: item.numeroConteneur,
      codeArticle: dataset.x3OrdresFabrication.find((of) => of.numeroOF === item.numeroOF)?.codeArticleParent || '',
      designationArticle:
        dataset.x3OrdresFabrication.find((of) => of.numeroOF === item.numeroOF)?.designationArticle || '',
      ofDestination: item.numeroOF,
      codeTypeMatiere: item.codeTypeMatiere,
      quantiteReelle: item.poidsReelKg,
      statut: item.statut,
      observation: item.observation
    })),
    categories: dataset.inventaireCategories.map((item) => ({
      id: item.id,
      categorie: item.categorie,
      mois: Number(item.dateInventaire.slice(5, 7)),
      annee: Number(item.dateInventaire.slice(0, 4)),
      codeTypeMatiere: item.codeTypeMatiere,
      quantiteReelle: item.poidsReelKg,
      statut: item.statut,
      commentaire: item.commentaire
    })),
    fours: dataset.inventaireFours.map((item) => ({
      id: item.id,
      four: item.codeFour,
      dateInventaire: item.dateInventaire,
      codeTypeMatiere: item.codeTypeMatiere,
      quantite: item.quantite,
      statut: item.statut === 'Annule' ? 'Brouillon' : item.statut,
      observation: item.observation
    }))
  };
};

const centralReceptionViews = () =>
  testDataRepository.getDataset().receptionsUsinage2.map((item) => ({
    id: item.id,
    numeroOF: item.numeroOF,
    dateReception: item.dateReception,
    codeArticle: item.codeArticle,
    codeTypeMatiere: item.codeTypeMatiere,
    poidsKg: item.poidsTotalReceptionneKg,
    nombrePieces: item.nombrePiecesReceptionnees,
    atelierOrigine: item.atelierOrigine,
    poidsPhysique: item.poidsTotalReceptionneKg,
    designationArticle: item.designationArticle
  }));

const centralWasteViews = () =>
  testDataRepository.getDataset().dechetsFonderie.map((item) => ({
    id: item.id,
    dateDeclaration: item.dateDeclaration,
    semaine: item.semaine,
    codeArticle: '',
    designationArticle: '',
    codeTypeMatiere: item.codeTypeMatiere,
    quantiteKg: item.poidsDechetKg,
    typeDechet: item.typeDechet,
    commentaire: item.observation,
    statut: item.statut,
    utilisateur: item.declarantNom
  }));

const centralMovementViews = () =>
  testDataRepository.getDataset().x3MouvementsStock.map((item) => ({
    id: item.id,
    dateImputation: item.dateImputation,
    codeMatiere: item.codeArticle,
    designation: item.designationArticle,
    quantiteImportee: item.quantite,
    quantiteUtilisee: item.quantite,
    transaction: item.referenceOrigineX3,
    lot: item.numeroLot,
    pieceOrigine: item.numeroOF,
    designationOrigine: item.referenceOrigineX3,
    emplacement: item.emplacement,
    statut: 'Synchronisé',
    source: 'API Sage X3',
    utilisateur: item.utilisateurX3
  }));

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
        const bilans = calculateBilanFonderie(testDataRepository.getDataset());
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
        const dechets = centralWasteViews();
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
        const sorties = centralMovementViews();
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
        const { conteneurs, categories, fours } = centralInventoryViews();
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
      } else if (reportId === 6) {
        const receptions = centralReceptionViews();
        const bilans = calculateBilanFonderie(testDataRepository.getDataset());
        const alertesPoids = receptions.filter((rec) => {
          const difference = rec.poidsPhysique - rec.poidsKg;
          return rec.atelierOrigine === 'FONDERIE' && Math.abs(difference) >= BUSINESS_RULES.seuilAlertePoidsKg;
        });
        const alertesBilan = bilans.filter(
          (bilan) => Math.abs(bilan.tauxEcart) >= BUSINESS_RULES.seuilEcartPourcentage
        );
        const lines = [
          "Type d'Alerte;Reference;Date/Periode;Type Matiere;Valeur attendue;Valeur reelle;Ecart;Seuil;Statut;Commentaire"
        ];

        alertesPoids.forEach((rec) => {
          const difference = rec.poidsPhysique - rec.poidsKg;
          lines.push(
            `"Poids reception";"${rec.numeroOF}";"${rec.dateReception}";"${rec.codeTypeMatiere}";${rec.poidsKg.toFixed(2).replace('.', ',')};${rec.poidsPhysique.toFixed(2).replace('.', ',')};${difference.toFixed(2).replace('.', ',')};${BUSINESS_RULES.seuilAlertePoidsKg};"A verifier";"${rec.designationArticle}"`
          );
        });

        alertesBilan.forEach((bilan) => {
          lines.push(
            `"Ecart bilan";"${bilan.periode}";"${bilan.periode}";"${bilan.typeMatiere}";${bilan.inputTotal.toFixed(2).replace('.', ',')};${bilan.outputTotal.toFixed(2).replace('.', ',')};${bilan.ecartKg.toFixed(2).replace('.', ',')};${BUSINESS_RULES.seuilEcartPourcentage}%;"${bilan.statut}";"${bilan.justification || ''}"`
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
        const bilans = calculateBilanFonderie(testDataRepository.getDataset());
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
        const dechets = centralWasteViews();
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
        const receptions = centralReceptionViews();
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
              `${r.numeroOF.padEnd(7)} | ${r.dateReception.padEnd(10)} | ${r.codeArticle.padEnd(11)} | ${r.codeTypeMatiere.padEnd(8)} | ${r.poidsKg.toLocaleString('fr-FR').padStart(10)} | ${r.nombrePieces.toString().padStart(6)} | ${r.atelierOrigine}`
          ),
          subSeparator,
          `Total Réceptions Enregistrées: ${receptions.reduce((s, r) => s + r.poidsKg, 0).toLocaleString('fr-FR')} kg`,
          separator
        ].join('\n');
      } else if (reportId === 5) {
        const { conteneurs, categories, fours } = centralInventoryViews();
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
      } else if (reportId === 6) {
        const receptions = centralReceptionViews();
        const bilans = calculateBilanFonderie(testDataRepository.getDataset());
        const alertesPoids = receptions
          .map((rec) => ({ ...rec, difference: rec.poidsPhysique - rec.poidsKg }))
          .filter(
            (rec) => rec.atelierOrigine === 'FONDERIE' && Math.abs(rec.difference) >= BUSINESS_RULES.seuilAlertePoidsKg
          );
        const alertesBilan = bilans.filter(
          (bilan) => Math.abs(bilan.tauxEcart) >= BUSINESS_RULES.seuilEcartPourcentage
        );

        txtContent = [
          separator,
          `             RAPPORT ALERTES BILAN MATIERE - SOPAL UAP2`,
          `             Genere le: ${new Date().toLocaleDateString('fr-FR')}`,
          separator,
          '',
          `Seuil poids reception: ${BUSINESS_RULES.seuilAlertePoidsKg.toLocaleString('fr-FR')} kg`,
          `Seuil ecart bilan: ${BUSINESS_RULES.seuilEcartPourcentage.toLocaleString('fr-FR')} %`,
          '',
          '--- ALERTES ECARTS DE POIDS RECEPTION ---',
          'OF      | Date       | Article     | Matiere  | Poids X3  | Poids reel | Ecart',
          subSeparator,
          ...alertesPoids.map(
            (rec) =>
              `${rec.numeroOF.padEnd(7)} | ${rec.dateReception.padEnd(10)} | ${rec.codeArticle.padEnd(11)} | ${rec.codeTypeMatiere.padEnd(8)} | ${rec.poidsKg.toLocaleString('fr-FR').padStart(9)} | ${rec.poidsPhysique.toLocaleString('fr-FR').padStart(10)} | ${rec.difference.toLocaleString('fr-FR').padStart(7)}`
          ),
          alertesPoids.length === 0 ? 'Aucune alerte poids.' : '',
          '',
          '--- ALERTES ECARTS BILAN MATIERE ---',
          'Periode | Matiere  | Input total | Output total | Ecart kg | Taux ecart | Statut',
          subSeparator,
          ...alertesBilan.map(
            (bilan) =>
              `${bilan.periode.padEnd(7)} | ${bilan.typeMatiere.padEnd(8)} | ${bilan.inputTotal.toLocaleString('fr-FR').padStart(11)} | ${bilan.outputTotal.toLocaleString('fr-FR').padStart(12)} | ${bilan.ecartKg.toLocaleString('fr-FR').padStart(8)} | ${bilan.tauxEcart.toFixed(2).padStart(10)}% | ${bilan.statut}`
          ),
          alertesBilan.length === 0 ? 'Aucune alerte bilan.' : '',
          subSeparator,
          `Total alertes poids: ${alertesPoids.length}`,
          `Total alertes bilan: ${alertesBilan.length}`,
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
