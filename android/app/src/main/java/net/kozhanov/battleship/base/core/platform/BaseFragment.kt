package net.kozhanov.battleship.base.core.platform

import android.os.Bundle
import android.view.View
import androidx.annotation.LayoutRes
import androidx.fragment.app.Fragment
import ru.openbank.accept.base.extensions.hideKeyboard
import net.kozhanov.battleship.base.extensions.launchAndCollectIn

abstract class BaseFragment<VIEW_STATE>(
    @LayoutRes contentLayoutId: Int
) : Fragment(contentLayoutId) {

    abstract val viewModel: BaseViewModel<VIEW_STATE>

    abstract fun setupUI()
    abstract fun render(viewState: VIEW_STATE)

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        setupObserve()
        setupUI()
    }

    open fun setupObserve() {
        viewModel.viewState.launchAndCollectIn(viewLifecycleOwner) { viewState ->
            render(viewState)
        }

        viewModel.singleEvent.launchAndCollectIn(viewLifecycleOwner) { event ->
            singleEvent(event)
        }
    }

    open fun singleEvent(event: SingleEvent) {
        // nothing
    }

    override fun onStop() {
        super.onStop()
        hideKeyboard()
    }
}
