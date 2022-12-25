package net.kozhanov.battleship.features.board.di

import net.kozhanov.battleship.base.core.data.GameRepository
import net.kozhanov.battleship.base.core.data.GameRepositoryImpl
import net.kozhanov.battleship.base.core.data.models.GameApi
import net.kozhanov.battleship.features.board.BoardViewModel
import okhttp3.OkHttpClient
import org.koin.androidx.viewmodel.dsl.viewModel
import org.koin.dsl.module
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

val boardModule = module {

/*    val httpInterceptor = HttpLoggingInterceptor()
    httpInterceptor.setLevel(HttpLoggingInterceptor.Level.BODY)*/

    Retrofit.Builder()
        .client(
            OkHttpClient.Builder()
                //.addInterceptor(httpInterceptor)
                .build()
        )
        .addConverterFactory(GsonConverterFactory.create())
        .baseUrl("https://postman-echo.com/")
        .build()
        .create(GameApi::class.java)



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
    //https://164.92.225.164/
    return Retrofit.Builder().baseUrl("http://192.168.1.10:5000/").client(okHttpClient)
        .addConverterFactory(GsonConverterFactory.create()).build()
}

fun provideOkHttpClient(): OkHttpClient {
    return OkHttpClient().newBuilder().build()
}

fun provideForecastApi(retrofit: Retrofit): GameApi = retrofit.create(GameApi::class.java)
