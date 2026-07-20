<script setup lang="ts">
import { computed } from 'vue';
import { AlertTriangle, ArrowDownToLine, ArrowUpFromLine, Scale } from 'lucide-vue-next';
import { formatNumber } from '@/utils/formatters';

type ComparisonItem = {
  typeMatiere: string;
  input: number;
  output: number;
  difference: number;
  tauxEcart: number;
  statut: string;
};

type StatusTone = 'success' | 'watch' | 'gap' | 'critical';

const props = defineProps<{
  series: ComparisonItem[];
  motionEnabled: boolean;
  highlightThreshold: number;
}>();

const getStatusMeta = (item: ComparisonItem): { label: string; tone: StatusTone } => {
  const absoluteGap = Math.abs(item.difference);
  if (absoluteGap > props.highlightThreshold) return { label: 'Critique', tone: 'critical' };
  if (item.statut === 'AvecEcart') return { label: 'Avec écart', tone: 'gap' };
  if (absoluteGap > 0.001) return { label: 'À surveiller', tone: 'watch' };
  return { label: 'Conforme', tone: 'success' };
};

const getLocalBarWidth = (item: ComparisonItem, value: number): string => {
  const maximum = Math.max(Math.abs(item.input), Math.abs(item.output), 1);
  const percentage = Math.min(100, (Math.abs(value) / maximum) * 100);
  return percentage === 0 ? '0%' : `${Math.max(2, percentage)}%`;
};

const getDifferenceBarWidth = (item: ComparisonItem): string => {
  const maximum = Math.max(Math.abs(item.input), Math.abs(item.output), 1);
  const percentage = Math.min(100, (Math.abs(item.difference) / maximum) * 100);
  return percentage === 0 ? '0%' : `${Math.max(3, percentage)}%`;
};

const criticalItems = computed(() =>
  props.series.filter((item) => getStatusMeta(item).tone === 'critical')
);

const formatSigned = (value: number): string => {
  const formatted = formatNumber(value, 0);
  return value > 0 ? `+${formatted}` : formatted;
};
</script>

<template>
  <section class="comparison-section dashboard-enter" aria-labelledby="comparison-title">
    <header class="comparison-header">
      <div>
        <span class="comparison-header__eyebrow">Analyse des flux</span>
        <h2 id="comparison-title">Comparatif du bilan matière</h2>
        <p>Input, output et écart par type de matière.</p>
      </div>
      <div class="comparison-header__legend" aria-label="Légende">
        <span><i class="legend-dot legend-dot--input"></i>Input</span>
        <span><i class="legend-dot legend-dot--output"></i>Output</span>
      </div>
    </header>

    <div v-if="props.series.length" class="material-grid">
      <article
        v-for="(item, index) in props.series"
        :key="item.typeMatiere"
        class="material-card"
        :style="{ '--card-delay': `${index * 90}ms` }"
      >
        <header class="material-card__header">
          <div>
            <h3>{{ item.typeMatiere }}</h3>
            <p>Input, output et écart</p>
          </div>
          <span class="status-badge" :class="`status-badge--${getStatusMeta(item).tone}`">
            <i aria-hidden="true"></i>
            {{ getStatusMeta(item).label }}
          </span>
        </header>

        <div class="flow-values">
          <section class="flow-value flow-value--input" aria-label="Valeur Input">
            <span class="flow-value__icon"><ArrowDownToLine class="h-4 w-4" aria-hidden="true" /></span>
            <div>
              <small>Input</small>
              <strong>{{ formatNumber(item.input, 0) }} <em>kg</em></strong>
            </div>
          </section>

          <section class="flow-value flow-value--output" aria-label="Valeur Output">
            <span class="flow-value__icon"><ArrowUpFromLine class="h-4 w-4" aria-hidden="true" /></span>
            <div>
              <small>Output</small>
              <strong>{{ formatNumber(item.output, 0) }} <em>kg</em></strong>
            </div>
          </section>
        </div>

        <section class="difference-block" :class="`difference-block--${getStatusMeta(item).tone}`">
          <div class="difference-block__heading">
            <span>Différence</span>
            <strong>{{ formatSigned(item.difference) }} <small>kg</small></strong>
          </div>
          <div
            class="difference-track"
            role="img"
            :aria-label="`Différence ${formatNumber(item.difference, 0)} kilogrammes, taux ${formatNumber(item.tauxEcart, 2)} pour cent`"
          >
            <span
              class="difference-fill"
              :class="{ 'bar-animated': props.motionEnabled }"
              :style="{ width: getDifferenceBarWidth(item) }"
            ></span>
          </div>
          <div class="difference-block__rate">
            <span>Taux d’écart</span>
            <strong>{{ formatNumber(item.tauxEcart, 2) }} %</strong>
          </div>
        </section>

        <section class="flow-comparison" aria-label="Comparatif Input et Output">
          <h4>Comparatif Input / Output</h4>

          <div class="flow-bar-line">
            <span>Input</span>
            <div
              class="flow-track"
              role="img"
              :aria-label="`Input ${formatNumber(item.input, 0)} kilogrammes`"
            >
              <i
                class="flow-fill flow-fill--input"
                :class="{ 'bar-animated': props.motionEnabled }"
                :style="{ width: getLocalBarWidth(item, item.input) }"
              ></i>
            </div>
            <strong>{{ formatNumber(item.input, 0) }} <small>kg</small></strong>
          </div>

          <div class="flow-bar-line">
            <span>Output</span>
            <div
              class="flow-track"
              role="img"
              :aria-label="`Output ${formatNumber(item.output, 0)} kilogrammes`"
            >
              <i
                class="flow-fill flow-fill--output"
                :class="{ 'bar-animated': props.motionEnabled }"
                :style="{ width: getLocalBarWidth(item, item.output) }"
              ></i>
            </div>
            <strong>{{ formatNumber(item.output, 0) }} <small>kg</small></strong>
          </div>
        </section>
      </article>
    </div>

    <div v-else class="comparison-empty">
      <Scale class="h-7 w-7" aria-hidden="true" />
      <div>
        <strong>Aucun flux à afficher</strong>
        <p>Les filtres sélectionnés ne retournent aucune ligne de bilan.</p>
      </div>
    </div>

    <section v-if="criticalItems.length" class="critical-alerts" aria-labelledby="critical-alerts-title">
      <h3 id="critical-alerts-title" class="sr-only">Alertes critiques du comparatif</h3>
      <article v-for="item in criticalItems" :key="`alert-${item.typeMatiere}`" class="critical-alert">
        <span class="critical-alert__icon"><AlertTriangle class="h-5 w-5" aria-hidden="true" /></span>
        <div>
          <h4>Alerte Écart Critique : {{ item.typeMatiere }}</h4>
          <p>
            Le type de matière présente un écart de
            <strong>{{ formatNumber(Math.abs(item.difference), 0) }} kg</strong>, soit
            <strong>{{ formatNumber(item.tauxEcart, 2) }} %</strong>.
          </p>
        </div>
      </article>
    </section>
  </section>
</template>

<style scoped>
.comparison-section {
  overflow: hidden;
  border: 1px solid #dbe5f1;
  border-radius: 1.125rem;
  background: #fff;
  box-shadow:
    0 1px 2px rgb(15 23 42 / 0.04),
    0 20px 46px -34px rgb(30 64 175 / 0.3);
  animation: section-enter 0.5s ease both;
}

.comparison-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1.5rem;
  padding: 1.5rem 1.75rem 1.25rem;
  border-bottom: 1px solid #e5edf6;
}

.comparison-header__eyebrow {
  display: block;
  margin-bottom: 0.35rem;
  color: #2563eb;
  font-size: 0.65rem;
  font-weight: 800;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.comparison-header h2 {
  color: #0f172a;
  font-size: clamp(1.15rem, 2vw, 1.4rem);
  font-weight: 850;
  letter-spacing: -0.025em;
}

.comparison-header p {
  margin-top: 0.35rem;
  color: #64748b;
  font-size: 0.8rem;
}

.comparison-header__legend {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  gap: 1rem;
  padding: 0.55rem 0.75rem;
  border: 1px solid #dbe5f1;
  border-radius: 0.7rem;
  background: #f8fafc;
  color: #334155;
  font-size: 0.7rem;
  font-weight: 750;
}

.comparison-header__legend span {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
}

.legend-dot {
  width: 0.55rem;
  height: 0.55rem;
  border-radius: 999px;
}

.legend-dot--input {
  background: #2563eb;
}

.legend-dot--output {
  background: #059669;
}

.material-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 1rem;
  padding: 1.25rem;
}

.material-card {
  display: flex;
  min-width: 0;
  height: 100%;
  flex-direction: column;
  padding: 1.15rem;
  border: 1px solid #dce6f1;
  border-radius: 1rem;
  background: linear-gradient(180deg, #fff 0%, #fbfdff 100%);
  box-shadow: 0 16px 34px -30px rgb(15 23 42 / 0.55);
  animation: card-enter 0.55s ease both;
  animation-delay: var(--card-delay);
  transition:
    transform 0.2s ease,
    border-color 0.2s ease,
    box-shadow 0.2s ease;
}

.material-card:hover {
  transform: translateY(-2px);
  border-color: #bfdbfe;
  box-shadow: 0 20px 38px -28px rgb(30 64 175 / 0.34);
}

.material-card__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.75rem;
  min-height: 3.5rem;
}

.material-card__header h3 {
  color: #172554;
  font-family: var(--font-mono);
  font-size: 1.05rem;
  font-weight: 900;
  letter-spacing: -0.02em;
}

.material-card__header p {
  margin-top: 0.25rem;
  color: #64748b;
  font-size: 0.68rem;
  font-weight: 650;
}

.status-badge {
  display: inline-flex;
  flex: 0 0 auto;
  align-items: center;
  gap: 0.4rem;
  padding: 0.38rem 0.58rem;
  border: 1px solid transparent;
  border-radius: 999px;
  font-size: 0.64rem;
  font-weight: 850;
  white-space: nowrap;
  animation: badge-enter 0.4s ease both;
}

.status-badge i {
  width: 0.42rem;
  height: 0.42rem;
  border-radius: 999px;
  background: currentColor;
}

.status-badge--success {
  border-color: #a7f3d0;
  background: #ecfdf5;
  color: #047857;
}

.status-badge--watch {
  border-color: #fed7aa;
  background: #fff7ed;
  color: #c2410c;
}

.status-badge--gap {
  border-color: #fde68a;
  background: #fffbeb;
  color: #a16207;
}

.status-badge--critical {
  border-color: #fecdd3;
  background: #fff1f2;
  color: #be123c;
}

.flow-values {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.7rem;
  margin-top: 1rem;
}

.flow-value {
  display: flex;
  min-width: 0;
  align-items: center;
  gap: 0.6rem;
  padding: 0.85rem;
  border: 1px solid transparent;
  border-radius: 0.75rem;
}

.flow-value--input {
  border-color: #bfdbfe;
  background: #eff6ff;
  color: #1d4ed8;
}

.flow-value--output {
  border-color: #a7f3d0;
  background: #ecfdf5;
  color: #047857;
}

.flow-value__icon {
  display: grid;
  width: 2rem;
  height: 2rem;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 0.55rem;
  background: #fff;
  box-shadow: 0 5px 14px -10px currentColor;
}

.flow-value div {
  min-width: 0;
}

.flow-value small {
  display: block;
  font-size: 0.6rem;
  font-weight: 850;
  letter-spacing: 0.07em;
  text-transform: uppercase;
}

.flow-value strong {
  display: block;
  margin-top: 0.2rem;
  overflow: hidden;
  font-family: var(--font-mono);
  font-size: clamp(1rem, 1.8vw, 1.35rem);
  font-variant-numeric: tabular-nums;
  letter-spacing: -0.045em;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.flow-value strong em {
  font-family: var(--font-sans);
  font-size: 0.62rem;
  font-style: normal;
  font-weight: 700;
  letter-spacing: 0;
  opacity: 0.72;
}

.difference-block {
  --status-color: #047857;
  --status-soft: #ecfdf5;
  margin-top: 0.75rem;
  padding: 0.8rem 0.85rem;
  border: 1px solid color-mix(in srgb, var(--status-color) 20%, #e2e8f0);
  border-radius: 0.75rem;
  background: color-mix(in srgb, var(--status-soft) 76%, #fff);
}

.difference-block--watch {
  --status-color: #c2410c;
  --status-soft: #fff7ed;
}

.difference-block--gap {
  --status-color: #a16207;
  --status-soft: #fffbeb;
}

.difference-block--critical {
  --status-color: #be123c;
  --status-soft: #fff1f2;
}

.difference-block__heading,
.difference-block__rate {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.difference-block__heading > span,
.difference-block__rate > span {
  color: #475569;
  font-size: 0.64rem;
  font-weight: 800;
}

.difference-block__heading strong {
  color: var(--status-color);
  font-family: var(--font-mono);
  font-size: 0.88rem;
  font-variant-numeric: tabular-nums;
}

.difference-block__heading strong small {
  font-family: var(--font-sans);
  font-size: 0.58rem;
}

.difference-track,
.flow-track {
  overflow: hidden;
  border-radius: 999px;
  background: #e2e8f0;
  box-shadow: inset 0 1px 2px rgb(15 23 42 / 0.07);
}

.difference-track {
  height: 0.55rem;
  margin: 0.65rem 0 0.45rem;
}

.difference-fill {
  display: block;
  height: 100%;
  border-radius: inherit;
  background: var(--status-color);
  transform-origin: left;
}

.difference-block__rate strong {
  color: var(--status-color);
  font-family: var(--font-mono);
  font-size: 0.68rem;
}

.flow-comparison {
  display: grid;
  gap: 0.58rem;
  margin-top: auto;
  padding-top: 1rem;
}

.flow-comparison h4 {
  color: #334155;
  font-size: 0.65rem;
  font-weight: 850;
  letter-spacing: 0.025em;
}

.flow-bar-line {
  display: grid;
  grid-template-columns: 2.9rem minmax(0, 1fr) auto;
  align-items: center;
  gap: 0.45rem;
}

.flow-bar-line > span {
  color: #64748b;
  font-size: 0.6rem;
  font-weight: 750;
}

.flow-bar-line > strong {
  min-width: 4.6rem;
  color: #334155;
  font-family: var(--font-mono);
  font-size: 0.62rem;
  font-variant-numeric: tabular-nums;
  text-align: right;
}

.flow-bar-line > strong small {
  color: #94a3b8;
  font-family: var(--font-sans);
  font-size: 0.54rem;
}

.flow-track {
  height: 0.65rem;
}

.flow-fill {
  display: block;
  height: 100%;
  border-radius: inherit;
  transform-origin: left;
}

.flow-fill--input {
  background: linear-gradient(90deg, #60a5fa, #1d4ed8);
}

.flow-fill--output {
  background: linear-gradient(90deg, #34d399, #047857);
}

.bar-animated {
  animation: bar-grow 0.7s cubic-bezier(0.2, 0.75, 0.25, 1) both;
  animation-delay: var(--card-delay);
}

.critical-alerts {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 0.8rem;
  padding: 0 1.25rem 1.25rem;
}

.critical-alert {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 0.95rem 1rem;
  border: 1px solid #fecdd3;
  border-radius: 0.8rem;
  background: #fff1f2;
}

.critical-alert__icon {
  display: grid;
  width: 2.2rem;
  height: 2.2rem;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 0.65rem;
  background: #fff;
  color: #be123c;
}

.critical-alert h4 {
  color: #9f1239;
  font-size: 0.75rem;
  font-weight: 850;
}

.critical-alert p {
  margin-top: 0.25rem;
  color: #475569;
  font-size: 0.68rem;
  line-height: 1.5;
}

.critical-alert p strong {
  color: #9f1239;
}

.comparison-empty {
  display: flex;
  align-items: flex-start;
  gap: 0.8rem;
  margin: 1.25rem;
  padding: 1rem;
  border: 1px solid #bfdbfe;
  border-radius: 0.8rem;
  background: #eff6ff;
  color: #2563eb;
}

.comparison-empty strong {
  color: #172554;
  font-size: 0.82rem;
}

.comparison-empty p {
  margin-top: 0.2rem;
  color: #64748b;
  font-size: 0.72rem;
}

@keyframes section-enter {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes card-enter {
  from {
    opacity: 0;
    transform: translateY(12px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes badge-enter {
  from {
    opacity: 0;
    transform: scale(0.94);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}

@keyframes bar-grow {
  from {
    transform: scaleX(0);
  }
  to {
    transform: scaleX(1);
  }
}

@media (min-width: 768px) {
  .material-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .critical-alerts {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (min-width: 1280px) {
  .material-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 640px) {
  .comparison-header {
    align-items: flex-start;
    flex-direction: column;
    padding: 1.2rem;
  }

  .comparison-header__legend {
    width: 100%;
    justify-content: center;
  }

  .material-grid {
    padding: 1rem;
  }

  .flow-values {
    grid-template-columns: minmax(0, 1fr);
  }

  .flow-bar-line {
    grid-template-columns: 2.8rem minmax(0, 1fr);
  }

  .flow-bar-line > strong {
    grid-column: 2;
    text-align: left;
  }

  .critical-alerts {
    padding: 0 1rem 1rem;
  }
}

@media (prefers-reduced-motion: reduce) {
  .comparison-section,
  .material-card,
  .status-badge,
  .bar-animated {
    animation: none;
  }
}
</style>
