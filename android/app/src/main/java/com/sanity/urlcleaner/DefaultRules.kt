package com.sanity.urlcleaner

object DefaultRules {
    fun create(): List<UrlRule> {
        val rules = mutableListOf<UrlRule>()

        rules += global("[?&](utm_[a-zA-Z0-9_]+=[^&]*)")
        rules += global("[?&](fbclid=[^&]*)")
        rules += global("[?&](gclid=[^&]*)")
        rules += global("[?&](msclkid=[^&]*)")
        rules += global("[?&](twclid=[^&]*)")
        rules += global("[?&](dclid=[^&]*)")
        rules += global("[?&](gbraid=[^&]*)")
        rules += global("[?&](wbraid=[^&]*)")
        rules += global("[?&](srsltid=[^&]*)")
        rules += global("[?&](mc_[a-z]+=[^&]*)")

        addPlatform(rules, listOf("youtube.com", "youtu.be"),
            "si=[^&]*", "feature=[^&]*", "pp=[^&]*", "embeds_referring_euri=[^&]*")
        addPlatform(rules, listOf("amazon.com", "amazon.co.uk", "amazon.de", "amazon.fr", "amazon.ca",
            "amazon.es", "amazon.it", "amazon.co.jp", "amzn.to", "a.co"),
            "tag=[^&]*", "linkCode=[^&]*", "ref_=[^&]*", "ascsubtag=[^&]*", "creative=[^&]*",
            "creativeASIN=[^&]*", "linkId=[^&]*", "pd_rd_w=[^&]*", "pd_rd_wg=[^&]*", "pd_rd_r=[^&]*",
            "pf_rd_p=[^&]*", "pf_rd_r=[^&]*")
        addPlatform(rules, listOf("google.com", "google.co.uk", "google.de", "google.fr", "google.ca", "google.com.au"),
            "ved=[^&]*", "usg=[^&]*", "sa=[^&]*", "source=[^&]*", "gs_lcp=[^&]*", "ei=[^&]*",
            "sclient=[^&]*", "oq=[^&]*", "gs_l=[^&]*", "cad=[^&]*")
        addPlatform(rules, listOf("facebook.com", "fb.com", "fb.watch", "m.facebook.com"),
            "ref=[^&]*", "refid=[^&]*", "__tn__=[^&]*", "__cft__=[^&]*", "mibextid=[^&]*")
        addPlatform(rules, listOf("instagram.com"), "igsh=[^&]*", "ig_rid=[^&]*")
        addPlatform(rules, listOf("tiktok.com", "vm.tiktok.com", "www.tiktok.com"),
            "_t=[^&]*", "_r=[^&]*", "share_app_id=[^&]*", "share_link_id=[^&]*",
            "tt_medium=[^&]*", "tt_source=[^&]*", "is_from_webapp=[^&]*")
        addPlatform(rules, listOf("x.com", "twitter.com", "t.co", "mobile.twitter.com"),
            "s=[^&]*", "ref_src=[^&]*", "ref_url=[^&]*", "t=[^&]*")
        addPlatform(rules, listOf("reddit.com", "old.reddit.com", "www.reddit.com", "redd.it", "new.reddit.com"),
            "share_id=[^&]*", "ref_source=[^&]*", "ref_campaign=[^&]*", "embed=[^&]*")

        return rules
    }

    private fun global(regex: String) = UrlRule(domain = "*", regex = regex)

    private fun addPlatform(rules: MutableList<UrlRule>, domains: List<String>, vararg params: String) {
        for (domain in domains) {
            for (param in params) {
                rules.add(UrlRule(domain = domain, regex = "[?&]($param)"))
            }
        }
    }
}
