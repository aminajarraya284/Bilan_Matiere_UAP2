<script setup lang="ts">
import AppSidebar from './AppSidebar.vue';
import AppHeader from './AppHeader.vue';
import AppDialog from '@/components/ui/AppDialog.vue';
import AppToast from '@/components/ui/AppToast.vue';
</script>

<template>
  <div class="app-layout flex h-dvh min-h-screen w-full overflow-hidden bg-[var(--color-canvas)] font-sans">
    <AppSidebar class="sidebar" />

    <div class="main-content flex flex-col min-w-0 min-h-0 overflow-hidden">
      <AppHeader />

      <main class="app-workspace min-h-0 min-w-0 flex-1 overflow-x-hidden overflow-y-auto p-4 sm:p-5 lg:p-6 xl:p-8">
        <router-view v-slot="{ Component, route }">
          <Transition name="page-motion" mode="out-in">
            <component :is="Component" :key="route.path" />
          </Transition>
        </router-view>
      </main>
    </div>

    <AppDialog />
    <AppToast />
  </div>
</template>
