<script setup lang="ts">
import AppSidebar from './AppSidebar.vue';
import AppHeader from './AppHeader.vue';
import AppDialog from '@/components/ui/AppDialog.vue';
</script>

<template>
  <div class="flex h-screen w-screen overflow-hidden bg-slate-100 font-sans">
    <!-- Navigation Sidebar -->
    <AppSidebar />

    <!-- Main Section -->
    <div class="flex-1 flex flex-col min-w-0 h-screen overflow-hidden">
      <!-- Top App Header -->
      <AppHeader />

      <!-- Workspace Body (Router View) -->
      <main class="flex-1 p-6 overflow-y-auto min-w-0">
        <router-view />
      </main>

      <!-- Status Bar / Footer -->
      <footer
        class="h-10 bg-white border-t border-slate-200 flex items-center justify-between px-8 text-[10px] text-slate-500 font-semibold shrink-0"
      >
        <div class="flex gap-4">
          <span>Système : <span class="text-blue-700 font-bold uppercase">Opérationnel</span></span>
          <span>Synchronisation Sage X3 : <span class="text-slate-700">Aujourd'hui à 08:45</span></span>
        </div>
        <div class="uppercase tracking-widest text-[9px] font-bold">
          SOPAL Bilan Matière UAP2 - v1.0.4 - Built with Vue 3
        </div>
      </footer>
    </div>

    <AppDialog />
  </div>
</template>
