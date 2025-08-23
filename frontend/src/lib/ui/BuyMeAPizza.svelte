<script>
    import { isDarkMode } from "$lib/stores/theme";

</script>
<div class="flex justify-center">
    <a href="https://www.buymeacoffee.com/hodgeswt">
        <span
            class="items-center px-4 py-2 bg-gradient-to-r from-indigo-600 to-purple-800 border-indigo-800 {$isDarkMode ? 'text-gray-300' : 'text-white'} font-semibold rounded-lg" data-testid="buymeapizza-text"
        >
            🍕 buy me a pizza
        </span>
    </a>
</div>
