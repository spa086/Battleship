package net.kozhanov.battleship.base.extensions

import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleOwner
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import arrow.core.Either
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.channels.ReceiveChannel
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.catch
import kotlinx.coroutines.launch

inline fun <T> Flow<T>.launchAndCollectIn(
    owner: LifecycleOwner,
    minActiveState: Lifecycle.State = Lifecycle.State.STARTED,
    crossinline action: suspend CoroutineScope.(T) -> Unit,
) = owner.lifecycleScope.launch {
    owner.repeatOnLifecycle(minActiveState) {
        collect {
            action(it)
        }
    }
}

inline fun <T> MutableStateFlow<T>.update(function: () -> T) {
    while (true) {
        val prevValue = value
        val nextValue = function()
        if (compareAndSet(prevValue, nextValue)) {
            return
        }
    }
}

fun <T> Flow<Either<Throwable, T>>.attempt() = this.catch { exception ->
    emit(Either.Left(exception))
}

fun <T> T?.trySendFromChannel(): ReceiveChannel<T?> {
    return Channel<T?>(1).apply {
        this@trySendFromChannel?.let { value ->
            this.trySend(value)
        }
    }
}
