package net.kozhanov.battleship.features.board

import android.content.Context
import android.graphics.BitmapFactory
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.util.AttributeSet
import android.view.MotionEvent
import android.view.View
import android.widget.Toast
import net.kozhanov.battleship.R
import net.kozhanov.battleship.base.core.data.models.Ship
import net.kozhanov.battleship.base.core.data.models.Ship.Deck
import net.kozhanov.battleship.features.board.BoardViewState.State.Board
import timber.log.Timber
import kotlin.math.min

class SeaBattleView @JvmOverloads constructor(
    context: Context, attrs: AttributeSet? = null, defStyleAttr: Int = 0
) : View(context, attrs, defStyleAttr) {

    companion object {
        private const val BOARD_SIZE = 10
        private val BLUE_PAINT = Paint().apply { color = Color.BLUE }
        private val RED_PAINT = Paint().apply { color = Color.RED }
    }

    private var boardWidth = 0
    private var cellSize = 0F
    private var ships: List<Ship> = emptyList()
    private var onCellClickListener: ((Int, Int) -> Unit)? = null

    override fun onMeasure(widthMeasureSpec: Int, heightMeasureSpec: Int) {
        super.onMeasure(widthMeasureSpec, heightMeasureSpec)

        val width = MeasureSpec.getSize(widthMeasureSpec)
        val height = MeasureSpec.getSize(heightMeasureSpec)
        val size = min(width, height)

        setMeasuredDimension(size, size)
    }

    override fun onSizeChanged(width: Int, height: Int, oldWidth: Int, oldHeight: Int) {
        super.onSizeChanged(width, height, oldWidth, oldHeight)

        boardWidth = width
        cellSize = boardWidth / BOARD_SIZE.toFloat()
    }

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)
        Timber.d("redrawing")

        for (column in 0 until BOARD_SIZE) {
            for (row in 0 until BOARD_SIZE) {
                val left = column * cellSize
                val top = row * cellSize
                val right = left + cellSize
                val bottom = top + cellSize

                canvas.drawRect(left, top, right, bottom, BLUE_PAINT)
                Timber.d("draved blue $left, $top, $right, $bottom")

            }
        }


        Timber.d("drawing ships $ships")
        for (ship in ships) {
            for (cell in ship.decks) {
                Timber.d("drawing cell $cell")
                val left = cell.x * cellSize
                val top = cell.y * cellSize
                val right = left + cellSize
                val bottom = top + cellSize
                Timber.d("draved red $left, $top, $right, $bottom")

                canvas.drawRect(left, top, right, bottom, RED_PAINT)
            }
        }
    }

    override fun onTouchEvent(event: MotionEvent): Boolean {
        if (event.action == MotionEvent.ACTION_DOWN) {
            val column = event.x.toInt() / cellSize.toInt()
            val row = event.y.toInt() / cellSize.toInt()

            onCellClickListener?.invoke(column, row)
        }

        return true
    }

    fun setShips(ships: List<Ship>) {
        this.ships = ships
        invalidate()
    }

    fun setOnCellClickListener(listener: (Int, Int) -> Unit) {
        onCellClickListener = listener
    }
}