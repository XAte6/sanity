package com.sanity.urlcleaner

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test

class UrlCleanerTest {
    private val rules = DefaultRules.create()

    @Test
    fun tryClean_removesIsFromYoutubeShort() {
        val url = "https://youtube.com/shorts/gKsejCCCRO4?is=tevSWXzeADhnMnTR"
        val cleaned = UrlCleaner.tryClean(url, rules)
        assertEquals("https://youtube.com/shorts/gKsejCCCRO4", cleaned)
    }

    @Test
    fun tryClean_removesSiFromYoutubeShort() {
        val url = "https://youtube.com/shorts/dQw4w9WgXcQ?si=abc123xyz"
        val cleaned = UrlCleaner.tryClean(url, rules)
        assertEquals("https://youtube.com/shorts/dQw4w9WgXcQ", cleaned)
    }

    @Test
    fun tryClean_removesSiFromWwwYoutubeShort() {
        val url = "https://www.youtube.com/shorts/dQw4w9WgXcQ?si=abc123xyz"
        val cleaned = UrlCleaner.tryClean(url, rules)
        assertEquals("https://www.youtube.com/shorts/dQw4w9WgXcQ", cleaned)
    }

    @Test
    fun tryClean_removesSiWhenFollowedByOtherParams() {
        val url = "https://youtube.com/shorts/dQw4w9WgXcQ?si=abc123&feature=share"
        val cleaned = UrlCleaner.tryClean(url, rules)
        assertEquals("https://youtube.com/shorts/dQw4w9WgXcQ?feature=share", cleaned)
    }

    @Test
    fun extractHttpUrl_findsYoutubeShortInSharedText() {
        val text = "Watch this\nhttps://youtube.com/shorts/dQw4w9WgXcQ?si=abc123xyz"
        assertEquals(
            "https://youtube.com/shorts/dQw4w9WgXcQ?si=abc123xyz",
            UrlCleaner.extractHttpUrl(text)
        )
    }

    @Test
    fun extractHttpUrl_stripsTrailingPunctuation() {
        val text = "https://youtube.com/shorts/dQw4w9WgXcQ?si=abc123)."
        assertEquals(
            "https://youtube.com/shorts/dQw4w9WgXcQ?si=abc123",
            UrlCleaner.extractHttpUrl(text)
        )
    }

    @Test
    fun extractHttpUrl_handlesLeadingDirectionalMark() {
        val text = "\u200Ehttps://youtube.com/shorts/dQw4w9WgXcQ?si=abc123"
        assertNotNull(UrlCleaner.extractHttpUrl(text))
        assertEquals(
            "https://youtube.com/shorts/dQw4w9WgXcQ",
            UrlCleaner.tryClean(UrlCleaner.extractHttpUrl(text)!!, rules)
        )
    }
}
