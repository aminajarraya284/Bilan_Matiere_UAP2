<script setup lang="ts">
withDefaults(
  defineProps<{
    padding?: 'sm' | 'md' | 'lg';
    accent?: boolean;
  }>(),
  {
    padding: 'md',
    accent: false
  }
);
</script>

<template>
  <section class="surface-card app-card motion-card" :class="[`app-card--${padding}`, { 'app-card--accent': accent }]">
    <header v-if="$slots.header" class="app-card__header">
      <slot name="header" />
    </header>
    <div class="app-card__content"><slot /></div>
    <footer v-if="$slots.footer" class="app-card__footer">
      <slot name="footer" />
    </footer>
  </section>
</template>
