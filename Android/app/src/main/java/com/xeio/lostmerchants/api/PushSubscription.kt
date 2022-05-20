package com.xeio.lostmerchants.api

data class PushSubscription(
    var token: String = "",
    var server: String = "",
    var weiVoteThreshold: Int = 0,
    var weiNotify: Boolean = false,
    var rapportVoteThreshold: Int = 0,
    var legendaryRapportNotify: Boolean = false,
    var sendTestNotification: Boolean = false
)
