package net.kozhanov.battleship.base.core.platform

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.annotation.LayoutRes
import androidx.fragment.app.DialogFragment
import androidx.lifecycle.LifecycleOwner
import ru.openbank.accept.base.extensions.hideKeyboard
import net.kozhanov.battleship.base.extensions.launchAndCollectIn

abstract class BaseDialogFragment<VIEW_STATE>(
    @LayoutRes val contentLayoutId: Int? = null
) : DialogFragment() {

    abstract val viewModel: BaseViewModel<VIEW_STATE>

    open fun setupUI() {
        // NOTHING
    }

    open fun render(viewState: VIEW_STATE) {
        // NOTHING
    }

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View? =
        contentLayoutId?.let {
            LayoutInflater.from(context).inflate(contentLayoutId, null)
        }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        setupObserve()
        setupUI()
    }

    open fun lifeCycleOwnerForCollect(): LifecycleOwner = viewLifecycleOwner

    open fun setupObserve() {
        viewModel.viewState.launchAndCollectIn(lifeCycleOwnerForCollect()) { viewState ->
            render(viewState)
        }

        viewModel.singleEvent.launchAndCollectIn(lifeCycleOwnerForCollect()) { event ->
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
