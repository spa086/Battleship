package net.kozhanov.battleship.app

import android.app.Application
import net.kozhanov.battleship.features.board.di.boardModule
import net.kozhanov.battleship.features.board.di.networkModule
import org.koin.android.ext.koin.androidContext
import org.koin.android.ext.koin.androidLogger
import org.koin.core.context.startKoin
import timber.log.Timber

class ShipApp  : Application() {
    override fun onCreate() {
        super.onCreate()
        initLogger()
        initKoin()
    }

    private fun initLogger() {
        Timber.plant(Timber.DebugTree())
    }

    private fun initKoin() {
        startKoin {
            androidContext(this@ShipApp)
            androidLogger()
            modules(
                listOf(
                    networkModule,
                    boardModule
                )
            )
        }
    }
}