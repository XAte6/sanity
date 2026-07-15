package com.sanity.urlcleaner

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test

class UrlCleanerTest {
    private val rules = DefaultRules.parse(
        checkNotNull(javaClass.classLoader!!.getResourceAsStream("regex-rules.json"))
            .bufferedReader()
            .readText()
    ).rules

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
    fun tryClean_stripsEbayQueryString() {
        val url = "https://www.ebay.co.uk/itm/127958206592?_skw=ryzen&itmmeta=abc&hash=item1"
        val cleaned = UrlCleaner.tryClean(url, rules)
        assertEquals("https://www.ebay.co.uk/itm/127958206592", cleaned)
    }

    @Test
    fun tryClean_stripsEtsyQueryString() {
        val url = "https://www.etsy.com/listing/1234567890/widget?ref=share&utm_source=x"
        val cleaned = UrlCleaner.tryClean(url, rules)
        assertEquals("https://www.etsy.com/listing/1234567890/widget", cleaned)
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
