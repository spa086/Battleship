package net.kozhanov.battleship.base.core.platform

import androidx.annotation.MainThread
import net.kozhanov.battleship.base.core.platform.ViewModelsBus.EventsListenerSubscription

/**
 * Содержит классы для общения ViewModel'ей и только между собой.
 */
object ViewModelsBus {

    @Suppress("UnnecessaryAbstractClass")
    abstract class BaseOutput<OUTPUT_EVENT : Event> {

        private val subscribers = arrayListOf<EventsListener<OUTPUT_EVENT>>()

        @MainThread
        fun push(outputEvent: OUTPUT_EVENT) {
            subscribers.forEach { it.onNewEvent(outputEvent) }
        }

        @MainThread
        fun subscribe(
            outputsSubscriptions: OutputsSubscriptions,
            func: (OUTPUT_EVENT) -> Unit
        ): EventsListenerSubscription {
            subscribers.add(func)
            return EventsListenerSubscription {
                subscribers.remove(func)
            }.also { outputsSubscriptions.putSubscription(it) }
        }

        @Suppress("unused")
        @MainThread
        fun subscribe(func: (OUTPUT_EVENT) -> Unit): EventsListenerSubscription {
            subscribers.add(func)
            return EventsListenerSubscription {
                subscribers.remove(func)
            }
        }
    }

    fun interface EventsListener<EVENT : Event> {
        fun onNewEvent(event: EVENT)
    }

    fun interface EventsListenerSubscription {
        @MainThread
        fun unsubscribe()
    }

    class OutputsSubscriptions {
        private val subscriptions = arrayListOf<EventsListenerSubscription>()

        @MainThread
        fun putSubscription(subscription: EventsListenerSubscription) {
            subscriptions.add(subscription)
        }

        @MainThread
        fun unsubscribe() {
            subscriptions.forEach { it.unsubscribe() }
            subscriptions.clear()
        }
    }
}
