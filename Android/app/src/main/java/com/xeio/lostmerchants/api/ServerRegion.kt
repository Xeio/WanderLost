package com.xeio.lostmerchants.api

data class ServerRegion(
    val Name: String = "",
    val UtcOffset: String = "",
    val Servers: List<String> = List(0) { "" }
)
