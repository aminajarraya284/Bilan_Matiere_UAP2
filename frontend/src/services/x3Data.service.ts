import {
  testDataRepository,
  createManualX3Record as persistManualX3Record,
  type ManualX3Record
} from './testDataRepository.service';
import type { SopalTestDataset } from '@/mocks/sopalTestDataset';

export type X3ImportProfile = string | { profil?: string; source?: 'Excel' | 'X3Api' };

export const simulateImport = (_profile: X3ImportProfile): SopalTestDataset => testDataRepository.getDataset();

export const simulateSynchronization = (_profile: X3ImportProfile): SopalTestDataset =>
  testDataRepository.synchronizeX3();

export const createManualX3Record = (data: ManualX3Record): unknown => persistManualX3Record(data);

export const x3DataService = {
  simulateImport,
  simulateSynchronization,
  createManualX3Record
};
