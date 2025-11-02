<script setup>
import TournamentCard from "@/components/TournamentCard.vue";
import TopicCard from "@/components/TopicCard.vue";
import { ref } from "vue";

const tournaments = ref([
  {
    id: 1,
    name: "Spring Chess Championship",
    description:
      "Annual spring tournament for chess enthusiasts. Join players from around the world in this exciting competition.",
    startDate: "2024-04-01",
    endDate: "2024-04-15",
  },
  {
    id: 2,
    name: "Weekend Blitz Tournament",
    description: "Fast-paced blitz tournament perfect for quick games and intense competition.",
    startDate: "2024-03-20",
    endDate: "2024-03-22",
  },
  {
    id: 3,
    name: "Summer Grand Prix",
    description: "The most prestigious tournament of the year featuring top-ranked players.",
    startDate: "2024-06-10",
    endDate: "2024-06-25",
  },
]);

const topics = ref([
  {
    id: 1,
    name: "World History",
    authors: ["John Smith", "Jane Doe"],
    description:
      "Explore major historical events and their impact on modern civilization. From ancient empires to world wars.",
    isGuest: true,
    isPlayed: true,
  },
  {
    id: 2,
    name: "Science & Technology",
    authors: ["Alice Johnson"],
    description: "Dive into fascinating questions about physics, chemistry, biology, and cutting-edge technology.",
    isGuest: true,
    isPlayed: false,
  },
  {
    id: 3,
    name: "Pop Culture Trivia",
    authors: ["Bob Williams", "Sarah Brown"],
    description: "Test your knowledge of movies, music, TV shows, and celebrity gossip from the past decades.",
    isGuest: false,
    isPlayed: true,
  },
  {
    id: 4,
    name: "Geography Challenge",
    authors: ["Mike Davis"],
    description: "Journey across continents with questions about countries, capitals, landmarks, and cultures.",
    isGuest: false,
    isPlayed: false,
  },
]);

const handleTournamentSettings = (tournament) => {
  console.log("Settings for:", tournament.name);
  // open a settings modal or navigate to settings page
};

const handleTournamentParticipants = (tournament) => {
  console.log("Participants for:", tournament.name);
  // show participants list or navigate to participants page
};

const handleTournamentStart = (tournament) => {
  console.log("Starting tournament:", tournament.name);
  // start the tournament or navigate to tournament page
};

const handleTournamentDelete = (tournament) => {
  console.log("Delete tournament:", tournament.name);
  // show a confirmation dialog and then delete
  if (confirm(`Are you sure you want to delete "${tournament.name}"?`)) {
    const index = tournaments.value.findIndex((t) => t.id === tournament.id);
    if (index > -1) {
      tournaments.value.splice(index, 1);
    }
  }
};

const handleTopicView = (topic) => {
  console.log("View topic:", topic.name);
  // navigate to topic view or open modal
};

const handleTopicAuthors = (topic) => {
  console.log("Authors for topic:", topic.name);
  // show authors list or navigate to authors page
};

const handleTopicDelete = (topic) => {
  console.log("Delete topic:", topic.name);
  // show a confirmation dialog and then delete
  if (confirm(`Are you sure you want to delete "${topic.name}"?`)) {
    const index = topics.value.findIndex((t) => t.id === topic.id);
    if (index > -1) {
      topics.value.splice(index, 1);
    }
  }
};
</script>

<template>
  <main class="container mx-auto px-4 py-8">
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-8">
      <!-- Tournaments Section -->
      <div>
        <div class="mb-6">
          <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">Tournaments</h1>
        </div>

        <div class="space-y-4">
          <TournamentCard
            v-for="tournament in tournaments"
            :key="tournament.id"
            :tournament="tournament"
            @settings="handleTournamentSettings"
            @participants="handleTournamentParticipants"
            @start="handleTournamentStart"
            @delete="handleTournamentDelete"
            class="w-full"
          />
        </div>

        <div v-if="tournaments.length === 0" class="text-center py-12">
          <div class="text-gray-500 dark:text-gray-400">
            <i class="pi pi-trophy text-4xl mb-4 block"></i>
            <p class="text-lg">No tournaments available</p>
            <p class="text-sm">Check back later for upcoming tournaments</p>
          </div>
        </div>
      </div>

      <!-- Topics Section -->
      <div>
        <div class="mb-6">
          <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">Topics</h1>
        </div>

        <div class="space-y-4">
          <TopicCard
            v-for="topic in topics"
            :key="topic.id"
            :topic="topic"
            @view="handleTopicView"
            @authors="handleTopicAuthors"
            @delete="handleTopicDelete"
            class="w-full"
          />
        </div>

        <div v-if="topics.length === 0" class="text-center py-12">
          <div class="text-gray-500 dark:text-gray-400">
            <i class="pi pi-book text-4xl mb-4 block"></i>
            <p class="text-lg">No topics available</p>
            <p class="text-sm">Check back later for new topics</p>
          </div>
        </div>
      </div>
    </div>
  </main>
</template>
