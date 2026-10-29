<script lang="ts">
    import Skeleton from "$lib/components/ui/skeleton/skeleton.svelte";
</script>

<div class="min-w-3xl w-full">
  <Skeleton class="h-12 w-full mb-4" />
</div>
<div class="grid grid-cols-2 gap-4">
  {#each [1, 2, 3, 4] as _}
    <Skeleton class="flex-grow h-24 w-full rounded-rect" />
  {/each}
</div>