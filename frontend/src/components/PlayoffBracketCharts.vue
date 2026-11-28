<script setup>
import { LIMITS } from "@/domain/limits.js";
import { playoffBracketCharts } from "@/utils/bracket.js";
import { outcomeLabel, outcomeTagSeverity } from "@/utils/match.js";
import OrganizationChart from "primevue/organizationchart";
import Tag from "primevue/tag";
import { computed } from "vue";
import { RouterLink } from "vue-router";

const props = defineProps({
  tournamentId: {
    type: String,
    required: true,
  },
  format: {
    type: String,
    required: true,
  },
  matches: {
    type: Array,
    required: true,
  },
});

const charts = computed(() => playoffBracketCharts(props.format, props.matches));

const matchRoute = (match) => ({
  name: "match-view",
  params: {
    tournamentId: props.tournamentId,
    matchId: match.id,
  },
});

const roundsLabel = (match) => `${match.roundsCount ?? 0} / ${LIMITS.matches.roundsPerMatch[match.type]} rounds`;
</script>

<template>
  <div class="flex flex-col gap-10">
    <section v-for="chart in charts" :key="chart.key">
      <h3 class="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">{{ chart.title }}</h3>

      <div class="overflow-x-auto pb-2">
        <OrganizationChart :value="chart.root">
          <template #default="{ node }">
            <RouterLink v-if="node.data.match" :to="matchRoute(node.data.match)" class="block min-w-56 text-left">
              <div class="flex items-center justify-between gap-3 mb-2">
                <span class="font-semibold text-gray-900 dark:text-gray-100">
                  <i
                    v-if="node.data.match.confirmedAt"
                    v-tooltip.top="'Confirmed'"
                    class="pi pi-check-circle text-xs mr-1"
                  ></i>
                  {{ node.data.title }}
                </span>
                <span class="text-xs text-gray-500 dark:text-gray-400">{{ roundsLabel(node.data.match) }}</span>
              </div>

              <div class="grid grid-cols-[1fr_auto_auto] items-center gap-x-3 gap-y-1 text-sm">
                <template v-for="player in node.data.match.matchParticipants" :key="player.id">
                  <div class="flex flex-col">
                    <span class="font-medium text-gray-800 dark:text-gray-200">{{ player.username }}</span>
                    <span class="text-xs text-gray-500 dark:text-gray-400">{{ player.sourceLabel }}</span>
                  </div>
                  <span class="text-right text-gray-600 dark:text-gray-400">{{ player.scoreSum ?? "-" }}</span>
                  <span>
                    <Tag
                      v-if="outcomeLabel(player)"
                      :value="outcomeLabel(player)"
                      :severity="outcomeTagSeverity(player)"
                      class="text-xs"
                    />
                  </span>
                </template>
              </div>
            </RouterLink>

            <div v-else class="min-w-56 text-left">
              <div class="font-semibold text-gray-500 dark:text-gray-400 mb-2">{{ node.data.title }}</div>
              <ul class="flex flex-col gap-1 text-sm italic text-gray-500 dark:text-gray-400">
                <li v-for="(label, index) in node.data.slotLabels" :key="index">{{ label }}</li>
              </ul>
            </div>
          </template>
        </OrganizationChart>
      </div>
    </section>
  </div>
</template>
