package net.kozhanov.battleship.features.board.di

import net.kozhanov.battleship.base.core.data.GameRepository
import net.kozhanov.battleship.base.core.data.GameRepositoryImpl
import net.kozhanov.battleship.base.core.data.models.GameApi
import net.kozhanov.battleship.features.board.BoardViewModel
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import org.koin.androidx.viewmodel.dsl.viewModel
import org.koin.dsl.module
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

val boardModule = module {

    viewModel {
        BoardViewModel(get())
    }

    single<GameRepository> { GameRepositoryImpl(get()) }
}

val networkModule = module {
    factory { provideOkHttpClient() }
    factory { provideForecastApi(get()) }
    single { provideRetrofit(get()) }
}

fun provideRetrofit(okHttpClient: OkHttpClient): Retrofit {
    val httpInterceptor = HttpLoggingInterceptor()
    httpInterceptor.setLevel(HttpLoggingInterceptor.Level.BODY)
    return Retrofit.Builder().baseUrl(URL).client(okHttpClient)
        .addConverterFactory(GsonConverterFactory.create()).build()
}

fun provideOkHttpClient(): OkHttpClient {
    val httpInterceptor = HttpLoggingInterceptor()
    httpInterceptor.setLevel(HttpLoggingInterceptor.Level.BODY)
    return OkHttpClient().newBuilder().addInterceptor(httpInterceptor).build()
}

fun provideForecastApi(retrofit: Retrofit): GameApi = retrofit.create(GameApi::class.java)

private const val URL = "http://164.92.225.164:5000/"